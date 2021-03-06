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

        private readonly IDictionary<MonitoredRepository, Dictionary<string, string>> old =
            new Dictionary<MonitoredRepository, Dictionary<string, string>>();
        private readonly IDictionary<MonitoredRepository, Dictionary<string, string>> current =
            new Dictionary<MonitoredRepository, Dictionary<string, string>>();

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

            var eventsRetrieval = (from mr in monitoredRepositories
                                     select ProbeMonitoredRepositoriesState(mr)).ToArray();

            Task.WhenAll(eventsRetrieval);
        }

        public IEnumerable<MonitoredRepository> MonitoredRepositories
        {
            get { return monitoredRepositories.ToArray(); }
        }

        public async Task<IEnumerable<Event>> ProbeMonitoredRepositoriesState(MonitoredRepository mr)
        {
            return await Task.Run(() =>
            {
                MarkBookmarksOld(mr);
                var tips = RetrieveRemoteBranches(mr);
                CreateNewBookmarksFrom(mr, tips);
                var branchEvents = Analyze(mr);

                return Synchronise(mr, branchEvents);
            });
        }

        private void MarkBookmarksOld(MonitoredRepository mr)
        {
            old.ReplaceOrAdd(mr, current.GetOrDefault(mr));
            current.ReplaceOrAdd(mr, new Dictionary<string, string>());
        }

        private void CreateNewBookmarksFrom(MonitoredRepository mr, Dictionary<string, string> tips)
        {
            current.ReplaceOrAdd(mr, tips);
        }

        private Dictionary<string, string> RetrieveRemoteBranches(MonitoredRepository monitoredRepository)
        {
            tracer.WriteInformation("Retrieving remote tips from repository '{0}'", monitoredRepository.FriendlyName);

            var remoteTips = RetrieveRemoteTips(monitoredRepository);

            return remoteTips
                .Where(kvp => IsHeadOrTag(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private bool IsHeadOrTag(string refName)
        {
            if (refName.StartsWith("refs/heads/"))
            {
                return true;
            }

            if (refName.StartsWith("refs/tags/"))
            {
                return true;
            }

            return false;
        }

        private BranchEvent[] Analyze(MonitoredRepository monitoredRepository)
        {
            var oldState = old[monitoredRepository];
            var currentState = current[monitoredRepository];

            var newBranches = from tip in currentState
                              where !oldState.ContainsKey(tip.Key)
                              select tip;

            var modifiedBranches = from newTip in currentState
                                   where oldState.ContainsKey(newTip.Key) && oldState[newTip.Key] != newTip.Value
                                   select newTip;

            var droppedBranches = from tip in oldState
                                  where !currentState.ContainsKey(tip.Key)
                                  select tip;

            var events = new List<BranchEvent>();

            events.AddRange(newBranches
                .Select(kvp => new BranchEvent(kvp.Key, null, kvp.Value, BranchEventKind.Created)));

            events.AddRange(modifiedBranches
                .Select(kvp => new BranchEvent(kvp.Key, oldState[kvp.Key], kvp.Value, BranchEventKind.Updated)));

            events.AddRange(droppedBranches
                .Select(kvp => new BranchEvent(kvp.Key, kvp.Value, null, BranchEventKind.Deleted)));

            if (events.Count == 0)
            {
                return new BranchEvent[] { };
            }

            MarkCreationOfBranchesFromKnownCommits(events);
            MarkUpdationOfBranchesToKnownCommits(events);

            return events.ToArray();
        }

        private void MarkCreationOfBranchesFromKnownCommits(IEnumerable<BranchEvent> branchEvents)
        {
            foreach (var branchEvent in branchEvents.Where(b => !b.IsFullyAnalyzed && b.Kind == BranchEventKind.Created))
            {
                if (!IsCommitLocallyKnown(branchEvent.NewSha))
                {
                    continue;
                }

                branchEvent.MarkAsNewBranchFromKnownCommit();
            }
        }

        private void MarkUpdationOfBranchesToKnownCommits(IEnumerable<BranchEvent> branchEvents)
        {
            foreach (var branchEvent in branchEvents.Where(b => !b.IsFullyAnalyzed && b.Kind == BranchEventKind.Updated))
            {
                if (!IsCommitLocallyKnown(branchEvent.NewSha))
                {
                    continue;
                }

                branchEvent.MarkAsUpdatedBranchToAKnownCommit();
            }
        }

        private IEnumerable<Event> Synchronise(MonitoredRepository mr, BranchEvent[] branchEvents)
        {
            RemoveReferences(string.Format("refs/radar/{0}/*", mr.FriendlyName));

            var events = new List<Event>();

            var refspecs = branchEvents
                .Where(be => be.Kind != BranchEventKind.Deleted)
                .Select(createdOrUpdated => string.Format("{0}:refs/radar/{1}/{0}",
                        createdOrUpdated.CanonicalName, mr.FriendlyName)).ToList();

            if (refspecs.Count > 0)
            {
                tracer.WriteInformation("Retrieving commits from repository '{0}'", mr.FriendlyName);

                FetchCommitsFrom(mr, refspecs);
            }

            var toBeDeleted = branchEvents
                .Where(be => be.Kind == BranchEventKind.Deleted);

            foreach (var branchEvent in toBeDeleted)
            {
                var ev = branchEvent.BuildEvent(mr);
                ev.Time = DateTime.Now;

                // TODO: Which identity should we use when a branch is deleted
                ev.Identity = new Identity{ Name = "Unknown", Email = "dont@know.com" };

                events.Add(ev);
            }

            foreach (var branchEvent in branchEvents.Where(b => !b.IsFullyAnalyzed))
            {
                var result = RetrieveListOfCommittedShas(branchEvent);

                branchEvent.MarkWithNewCommits(result.Item1, result.Item2);
            }

            foreach (var branchEvent in branchEvents.Where(be => be.Kind != BranchEventKind.Deleted))
            {
                var ev = branchEvent.BuildEvent(mr);

                // TODO: Retrieve identity of committers and commit times
                ev.Time = DateTime.Now;
                ev.Identity = new Identity { Name = "S. Omeone", Email = "someone@somewhere.com" };

                events.Add(ev);
            }

            return events;
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

        private Tuple<bool, string[]> RetrieveListOfCommittedShas(BranchEvent branchEvent)
        {
            var @old = repository.Lookup<Commit>(branchEvent.OldSha);
            var @new = repository.Lookup<Commit>(branchEvent.NewSha);

            var merge = repository.Commits.FindMergeBase(@old, @new);
            var shas = repository.Commits.QueryBy(new CommitFilter {Since = @new, Until = @old}).Select(c => c.Sha).ToArray();

            return new Tuple<bool, string[]>(merge == null || merge != old, shas);
        }

        private bool IsCommitLocallyKnown(string sha)
        {
            return repository.Lookup<Commit>(sha) != null;
        }

        private void RemoveReferences(string glob)
        {
            var refs = repository.Refs.FromGlob(glob);

            foreach (var @ref in refs)
            {
                repository.Refs.Remove(@ref);
            }
        }

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

        #endregion
    }
}
