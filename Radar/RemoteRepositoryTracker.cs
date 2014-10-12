using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Radar
{
    public class RemoteRepositoryTracker
    {
        private readonly IRepository _repository;
        private readonly Action<MonitoredRepository, BranchEvent[]> _branchEventsNotifier;
        private readonly Action<ForkEvent[]> _forkEventsNotifier;
        private readonly Func<ICollection<MonitoredRepository>> _snoozedRetriever;
        private readonly Func<ICollection<MonitoredRepository>> _forksRetriever;
        private readonly ConcurrentBag<MonitoredRepository> _monitoredRepositories;


        public RemoteRepositoryTracker(
            IRepository repository,
            Func<ICollection<MonitoredRepository>> snoozedRetriever,
            Func<ICollection<MonitoredRepository>> forksRetriever,
            Action<ForkEvent[]> forkEventsNotifier,
            Action<MonitoredRepository, BranchEvent[]> branchEventsNotifier
            )
        {
            _repository = repository;
            _branchEventsNotifier = branchEventsNotifier ?? VoidBranchEventsNotifier;
            _forkEventsNotifier = forkEventsNotifier ?? VoidForkEventsNotifier;
            _snoozedRetriever = snoozedRetriever ?? EmptyRetriever;
            _forksRetriever = forksRetriever ?? EmptyRetriever;

            _monitoredRepositories = RetrieveRepositoriesToTrack().Result;
        }

        public IEnumerable<MonitoredRepository> MonitoredRepositories
        {
            get { return _monitoredRepositories.ToArray(); }
        }

        public void StartTracking(TrackingType type, TimeSpan interval)
        {
            
        }

        public void StopTracking(TrackingType type)
        {
            
        }

        public TrackingState TrackingState 
        {
            get { }
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
    }
}
