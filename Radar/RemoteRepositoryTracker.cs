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
        private Timer _branchTrackingTimer = null;
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
            _branchEventsNotifier = branchEventsNotifier ?? VoidBranchEventsNotifier;
            _forkEventsNotifier = forkEventsNotifier ?? VoidForkEventsNotifier;
            _snoozedRetriever = snoozedRetriever ?? EmptyRetriever;
            _forksRetriever = forksRetriever ?? EmptyRetriever;

            _monitoredRepositories = RetrieveRepositoriesToTrack().Result;

            _tracer.WriteInformation("Monitoring {0} repositories...", _monitoredRepositories.Count);

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

                    _branchTrackingTimer = new Timer(interval.TotalMilliseconds);
                    _branchTrackingTimer.Elapsed += ProbeMonitoredRepositoriesState;
                    _branchTrackingTimer.Start();

                    _tracer.WriteInformation("Started branch monitoring (Will occur every {0} seconds)...", interval.TotalSeconds);

                    break;

                case TrackingType.ForkMonitoring:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProbeMonitoredRepositoriesState(object sender, ElapsedEventArgs e)
        {
            foreach (var monitoredRepository in _monitoredRepositories)
            {
                _tracer.WriteInformation("Probing repository '{0}'", monitoredRepository.FriendlyName);
            }
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

                    _tracer.WriteInformation("Stopped branch monitoring...");

                    break;

                case TrackingType.ForkMonitoring:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException();
            }
        }

        public TrackingState TrackingState
        {
            get { return _state; }
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

        private MonitoredRepository[] IdentifyKnownRemotes()
        {
            IEnumerable<MonitoredRepository> mrs = _repository.Network.Remotes
                .Select(r => new MonitoredRepository(r.Url, r.Name, RepositoryOrigin.Remote));

            return mrs.ToArray();
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
    }
}
