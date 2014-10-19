using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radar
{
    public class TrackingState
    {
        private readonly Action<MonitoredRepository, BranchEvent[]> _branchEventsNotifier;
        private readonly ConcurrentDictionary<MonitoredRepository, Dictionary<string, string>> _state = new ConcurrentDictionary<MonitoredRepository, Dictionary<string, string>>();

        public TrackingState(Action<MonitoredRepository, BranchEvent[]> branchEventsNotifier)
        {
            _branchEventsNotifier = branchEventsNotifier;
        }

        public void Clear()
        {
            _state.Clear();
        }

        public async Task Add(MonitoredRepository monitoredRepository, Dictionary<string, string> remoteBranches)
        {
            if (!_state.ContainsKey(monitoredRepository))
            {
                _state.GetOrAdd(monitoredRepository, remoteBranches);
                return;
            }

            var old = _state[monitoredRepository];
            _state[monitoredRepository] = remoteBranches;

            await AnalyzeAndReport(monitoredRepository, old, remoteBranches);
        }

        private async Task AnalyzeAndReport(MonitoredRepository monitoredRepository,
            Dictionary<string, string> oldState, Dictionary<string, string> newState)
        {
            var newBranches = from tip in newState
                            where !oldState.ContainsKey(tip.Key)
                            select tip;

            var modifiedBranches = from newTip in newState
                                    where oldState.ContainsKey(newTip.Key) && oldState[newTip.Key] != newTip.Value
                                    select newTip;

            var droppedBranches = from tip in oldState
                                  where !newState.ContainsKey(tip.Key)
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
                return;
            }

            await Task.Run(() => _branchEventsNotifier(monitoredRepository, events.ToArray()));
        }
    }
}
