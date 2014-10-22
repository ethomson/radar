using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using Radar.Util;

namespace Radar.Tracking
{
    public class RemoteRepositoryTracker
    {
        private readonly IRepository repository;
        private readonly ITracer tracer;
        private readonly Func<ICollection<MonitoredRepository>> snoozedRetriever;
        private readonly Func<ICollection<MonitoredRepository>> forksRetriever;
        private readonly ConcurrentBag<MonitoredRepository> monitoredRepositories;
        private readonly ConcurrentDictionary<MonitoredRepository, Dictionary<string, string>> state =
            new ConcurrentDictionary<MonitoredRepository, Dictionary<string, string>>();

        public RemoteRepositoryTracker(
            IRepository repository,
            Func<ICollection<MonitoredRepository>> snoozedRetriever,
            Func<ICollection<MonitoredRepository>> forksRetriever,
            ITracer tracer
            )
        {
            this.repository = repository;
            this.tracer = tracer;
            this.snoozedRetriever = snoozedRetriever ?? EmptyRetriever;
            this.forksRetriever = forksRetriever ?? EmptyRetriever;

            monitoredRepositories = RetrieveRepositoriesToTrack().Result;

            this.tracer.WriteInformation("Monitoring {0} repositories...", monitoredRepositories.Count);

            tracer.WriteInformation("Retrieving initial state of monitored repositories...");
            ProbeMonitoredRepositoriesState();
        }

        private void Synchronise(MonitoredRepository monitoredRepository, BranchEvent[] events)
        {
            var refspecs = events
                .Where(be => be.Kind != BranchEventKind.Deleted)
                .Select(createdOrUpdated => string.Format("{1}:refs/radar/{0}/{2}",
                        monitoredRepository.FriendlyName, createdOrUpdated.CanonicalName, createdOrUpdated.Name)).ToList();

            if (refspecs.Count > 0)
            {
                tracer.WriteInformation("Retrieving commits from repository '{0}'", monitoredRepository.FriendlyName);

                FetchCommitsFrom(monitoredRepository, refspecs);
            }

            var toBeDeleted = events
                .Where(be => be.Kind == BranchEventKind.Deleted)
                .Select(deleted => string.Format("refs/radar/{0}/{1}",
                        monitoredRepository.FriendlyName, deleted.Name)).ToList();

            foreach (var refName in toBeDeleted)
            {
                tracer.WriteInformation("Dropping bookmark reference '{0}'", refName);

                RemoveBookmarkReference(refName);
            }
        }

        public IEnumerable<MonitoredRepository> MonitoredRepositories
        {
            get { return monitoredRepositories.ToArray(); }
        }

        public IDictionary<MonitoredRepository, BranchEvent[]> ProbeMonitoredRepositoriesState()
        {
            var events = new Dictionary<MonitoredRepository, BranchEvent[]>();

            foreach (var monitoredRepository in monitoredRepositories)
            {
                BranchEvent[] branchEvents = ProbeState(monitoredRepository);

                if (branchEvents.Length == 0)
                {
                    continue;
                }

                Synchronise(monitoredRepository, branchEvents);
                events.Add(monitoredRepository, branchEvents);
            }

            return events;
        }

        private BranchEvent[] ProbeState(MonitoredRepository monitoredRepository)
        {
            tracer.WriteInformation("Probing repository '{0}'", monitoredRepository.FriendlyName);

            var remoteBranches = RetrieveRemoteBranches(monitoredRepository);

            return Analyze(monitoredRepository, remoteBranches);
        }

        private BranchEvent[] Analyze(MonitoredRepository monitoredRepository, Dictionary<string, string> remoteBranches)
        {
            if (!state.ContainsKey(monitoredRepository))
            {
                state.GetOrAdd(monitoredRepository, remoteBranches);
                return new BranchEvent[] {};
            }

            var old = state[monitoredRepository];
            state[monitoredRepository] = remoteBranches;

            var newBranches = from tip in remoteBranches
                where !old.ContainsKey(tip.Key)
                select tip;

            var modifiedBranches = from newTip in remoteBranches
                where old.ContainsKey(newTip.Key) && old[newTip.Key] != newTip.Value
                select newTip;

            var droppedBranches = from tip in old
                where !remoteBranches.ContainsKey(tip.Key)
                select tip;


            var events = new List<BranchEvent>();

            events.AddRange(newBranches
                .Select(kvp => new BranchEvent(kvp.Key, null, kvp.Value, BranchEventKind.Created)));
            events.AddRange(modifiedBranches
                .Select(kvp => new BranchEvent(kvp.Key, old[kvp.Key], kvp.Value, BranchEventKind.Updated)));
            events.AddRange(droppedBranches
                .Select(kvp => new BranchEvent(kvp.Key, kvp.Value, null, BranchEventKind.Deleted)));

            if (events.Count == 0)
            {
                return new BranchEvent[] { };
            }

            tracer.WriteInformation("Changes have been detected in repository '{0}'", monitoredRepository.FriendlyName);

            return events.ToArray();
        }

        private Dictionary<string, string> RetrieveRemoteBranches(MonitoredRepository monitoredRepository)
        {
            var remoteTips = RetrieveRemoteTips(monitoredRepository);

            return remoteTips
                .Where(kvp => kvp.Key.StartsWith("refs/heads/"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private async Task<ConcurrentBag<MonitoredRepository>> RetrieveRepositoriesToTrack()
        {
            var known = await Task.FromResult(IdentifyKnownRemotes());
            var forks = await Task.FromResult(forksRetriever());
            var snoozed = await Task.FromResult(snoozedRetriever());

            var unknownForks = forks.Except(known);

            var monitoredRepositories = known.Union(unknownForks).Except(snoozed).Distinct();

            return new ConcurrentBag<MonitoredRepository>(monitoredRepositories);
        }

        private static ICollection<MonitoredRepository> EmptyRetriever()
        {
            return new MonitoredRepository[] { };
        }

        #region Git repository related interactions

        private MonitoredRepository[] IdentifyKnownRemotes()
        {
            IEnumerable<MonitoredRepository> mrs = repository.Network.Remotes
                .Select(r => new MonitoredRepository(r.Url, r.Name, RepositoryOrigin.Remote));

            return mrs.ToArray();
        }

        private Dictionary<string, string> RetrieveRemoteTips(MonitoredRepository monitoredRepository)
        {
            var remoteTips = repository.Network.ListReferences(monitoredRepository.Url)
                .ToDictionary(r => r.CanonicalName, r => r.TargetIdentifier);

            return remoteTips;
        }

        private void FetchCommitsFrom(MonitoredRepository monitoredRepository, IEnumerable<string> refspecs)
        {
            repository.Network.Fetch(monitoredRepository.Url, refspecs);
        }

        private void RemoveBookmarkReference(string refName)
        {
            repository.Refs.Remove(refName);
        }

        #endregion
    }
}
