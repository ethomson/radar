using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using LibGit2Sharp;

namespace Radar
{
    public class RemoteRepositoryTracker : IDisposable
    {
        private readonly IRepository _repository;
        private readonly ITrace _tracer;
        private readonly Action<MonitoredRepository, BranchEvent[]> _branchEventsNotifier;
        private readonly Action<ForkEvent[]> _forkEventsNotifier;
        private readonly Func<ICollection<MonitoredRepository>> _snoozedRetriever;
        private readonly Func<ICollection<MonitoredRepository>> _forksRetriever;
        private readonly ConcurrentBag<MonitoredRepository> _monitoredRepositories;
        private readonly Timer _forkTrackingTimer = null;
        private Timer _branchTrackingTimer;
        private readonly TrackingState _state;

        public RemoteRepositoryTracker(
            IRepository repository,
            Func<ICollection<MonitoredRepository>> snoozedRetriever,
            Func<ICollection<MonitoredRepository>> forksRetriever,
            Action<ForkEvent[]> forkEventsNotifier,
            Action<MonitoredRepository, BranchEvent[]> branchEventsNotifier,
            ITrace tracer
            )
        {
            _repository = repository;
            _tracer = tracer;
            _branchEventsNotifier = branchEventsNotifier;
            _forkEventsNotifier = forkEventsNotifier ?? VoidForkEventsNotifier;
            _snoozedRetriever = snoozedRetriever ?? EmptyRetriever;
            _forksRetriever = forksRetriever ?? EmptyRetriever;

            _state = _branchEventsNotifier == null ?
                new TrackingState(VoidBranchEventsNotifier) : new TrackingState(Intercept);

            _monitoredRepositories = RetrieveRepositoriesToTrack().Result;

            _tracer.WriteInformation("Monitoring {0} repositories...", _monitoredRepositories.Count);

        }

        private void Intercept(MonitoredRepository monitoredRepository, BranchEvent[] events)
        {
            var refspecs = events
                .Where(be => be.Kind != BranchEventKind.Deleted)
                .Select(createdOrUpdated => string.Format("{1}:refs/radar/{0}/{2}",
                        monitoredRepository.FriendlyName, createdOrUpdated.CanonicalName, createdOrUpdated.Name)).ToList();

            if (refspecs.Count > 0)
            {
                FetchCommitsFrom(monitoredRepository, refspecs);
            }

            var toBeDeleted = events
                .Where(be => be.Kind == BranchEventKind.Deleted)
                .Select(deleted => string.Format("refs/radar/{0}/{1}",
                        monitoredRepository.FriendlyName, deleted.Name)).ToList();

            foreach (var refName in toBeDeleted)
            {
                RemoveBookmarkReference(refName);
            }

            _branchEventsNotifier(monitoredRepository, events);
        }



        public IEnumerable<MonitoredRepository> MonitoredRepositories
        {
            get { return _monitoredRepositories.ToArray(); }
        }

        public void StartTracking(TrackingType type, TimeSpan interval)
        {
            switch (type)
            {
                case TrackingType.BranchMonitoring:
                    if (_branchTrackingTimer != null)
                    {
                        throw new InvalidOperationException();
                    }
                    _tracer.WriteInformation("Initializing branch monitoring...", interval.TotalSeconds);

                    _tracer.WriteInformation("Retrieving initial state of monitored repositories...", interval.TotalSeconds);
                    ProbeMonitoredRepositoriesState(null, null);

                    _branchTrackingTimer = new Timer(interval.TotalMilliseconds);
                    _branchTrackingTimer.Elapsed += ProbeMonitoredRepositoriesState;
                    _branchTrackingTimer.Start();

                    _tracer.WriteInformation("Registered recurrent branch monitoring (will occur every {0} seconds)...", interval.TotalSeconds);


                    break;

                case TrackingType.ForkMonitoring:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException();
            }
        }

        private async void ProbeMonitoredRepositoriesState(object sender, ElapsedEventArgs e)
        {
            foreach (var monitoredRepository in _monitoredRepositories)
            {
                var mr = monitoredRepository;
                await ProbeState(mr);
            }
        }

        private async Task ProbeState(MonitoredRepository monitoredRepository)
        {
            _tracer.WriteInformation("Probing repository '{0}'", monitoredRepository.FriendlyName);

            var remoteBranches = await RetrieveRemoteBranches(monitoredRepository);

            await _state.Add(monitoredRepository, remoteBranches);
        }

        private async Task<Dictionary<string, string>> RetrieveRemoteBranches(MonitoredRepository monitoredRepository)
        {
            var remoteTips = await RetrieveRemoteTips(monitoredRepository);

            return remoteTips
                .Where(kvp => kvp.Key.StartsWith("refs/heads/"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void StopTracking(TrackingType type)
        {
            switch (type)
            {
                case TrackingType.BranchMonitoring:
                    if (_branchTrackingTimer == null)
                    {
                        return;
                    }

                    _branchTrackingTimer.Elapsed -= ProbeMonitoredRepositoriesState;
                    _branchTrackingTimer.Dispose();
                    _branchTrackingTimer = null;

                    _state.Clear();

                    _tracer.WriteInformation("Stopped branch monitoring...");

                    break;

                case TrackingType.ForkMonitoring:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task<ConcurrentBag<MonitoredRepository>> RetrieveRepositoriesToTrack()
        {
            var known = await Task.FromResult(IdentifyKnownRemotes());
            var forks = await Task.FromResult(_forksRetriever());
            var snoozed = await Task.FromResult(_snoozedRetriever());

            var unknownForks = forks.Except(known);

            var monitoredRepositories = known.Union(unknownForks).Except(snoozed).Distinct();

            return new ConcurrentBag<MonitoredRepository>(monitoredRepositories);
        }



        private void VoidBranchEventsNotifier(MonitoredRepository mr, BranchEvent[] events)
        { }

        private static ICollection<MonitoredRepository> EmptyRetriever()
        {
            return new MonitoredRepository[] { };
        }

        private static void VoidForkEventsNotifier(ICollection<ForkEvent> events)
        { }

        public void Dispose()
        {
            StopTracking(TrackingType.BranchMonitoring);
        }

        #region Git repository related interactions

        private MonitoredRepository[] IdentifyKnownRemotes()
        {
            IEnumerable<MonitoredRepository> mrs = _repository.Network.Remotes
                .Select(r => new MonitoredRepository(r.Url, r.Name, RepositoryOrigin.Remote));

            return mrs.ToArray();
        }

        private async Task<Dictionary<string, string>> RetrieveRemoteTips(MonitoredRepository monitoredRepository)
        {
            var remoteTips = await Task.Run(() => _repository.Network.ListReferences(monitoredRepository.Url)
                .ToDictionary(r => r.CanonicalName, r => r.TargetIdentifier));

            return remoteTips;
        }

        private void FetchCommitsFrom(MonitoredRepository monitoredRepository, IEnumerable<string> refspecs)
        {
            _repository.Network.Fetch(monitoredRepository.Url, refspecs);
        }

        private void RemoveBookmarkReference(string refName)
        {
            _repository.Refs.Remove(refName);
        }

        #endregion
    }
}