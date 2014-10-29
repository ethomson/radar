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
        private Commit c1, c2, c3;

        public RemoteRepositoryTrackerFixture()
        {
            upstream = CreateUpstreamRepository();
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

        [Fact]
        public void CanDetectABranchCreatedFromAKnownCommit()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var tracker = BuildSUT(repo);

                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                PerformUpstreamChange(r => CreateBranchFromAKnownCommit(r, branchName, c2));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchCreatedFromKnownCommit, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(c2.Sha, e.Shas.Single());
            }
        }

        private void CreateBranchFromAKnownCommit(IRepository repo, string branchName, Commit commit)
        {
            repo.Branches.Add(branchName, commit);
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

        private IRepository CreateUpstreamRepository()
        {
            var tempPath = BuildTempPath(true);

            Repository.Init(tempPath);

            paths.Add(tempPath);

            var repo = new Repository(tempPath);

            c1 = AddRandomCommit(repo);
            c2 = AddRandomCommit(repo);
            c3 = AddRandomCommit(repo);

            return repo;
        }

        private Commit AddRandomCommit(IRepository repo)
        {
            var path = Path.Combine(repo.Info.WorkingDirectory, Guid.NewGuid().ToString());
            File.WriteAllText(path, Guid.NewGuid().ToString());
            repo.Index.Stage(path);

            var sig = new Signature("Name", "email", DateTimeOffset.Now);
            return repo.Commit(Guid.NewGuid().ToString(), sig, sig);
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
