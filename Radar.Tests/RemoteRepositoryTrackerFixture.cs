using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Radar.Tests.Helpers;
using Radar.Tracking;
using Radar.Util;
using Xunit;

namespace Radar.Tests
{
    public class RemoteRepositoryTrackerFixture : IDisposable
    {
        private readonly IRepository upstream;
        private readonly IList<string> paths = new List<string>();

        public RemoteRepositoryTrackerFixture()
        {
            var tempPath = BuildTempPath(true);

            Repository.Init(tempPath);

            paths.Add(tempPath);

            upstream = new Repository(tempPath);
        }

        [Fact]
        public void DoesNotReturnAnyEventWhenNoChangeHasOccured()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var tracker = BuildSUT(repo);

                PerformUpstreamChange(r => { });

                var evts = RetrieveActivity(tracker);

                Assert.Equal(0, evts.Count);
            }
        }

        private void PerformUpstreamChange(Action<IRepository> stateModifier)
        {
            stateModifier(upstream);
        }

        private static List<Event> RetrieveActivity(RemoteRepositoryTracker tracker)
        {
            var evts = new List<Event>();

            foreach (var monitoredRepository in tracker.MonitoredRepositories)
            {
                var enumerable = tracker.ProbeMonitoredRepositoriesState(monitoredRepository).Result;

                evts.AddRange(enumerable.Cast<Event>());
            }

            return evts;
        }

        private string CloneUpstream()
        {
            var tempPath = BuildTempPath();
            Repository.Clone(upstream.Info.Path, tempPath);

            paths.Add(tempPath);

            return tempPath;
        }

        private static string BuildTempPath(bool isUpStream = false)
        {
            return Path.Combine(Path.GetTempPath(),
                string.Format("radar-{0}-{1}", isUpStream ? "upstream" : "local", Guid.NewGuid()));
        }

        private static RemoteRepositoryTracker BuildSUT(Repository repo)
        {
            return new RemoteRepositoryTracker(new NullTracer(), repo, null, null);
        }

        public void Dispose()
        {
            upstream.Dispose();

            foreach (var p in paths)
            {
                DirectoryHelper.DeleteDirectory(p);
            }
        }
    }
}
