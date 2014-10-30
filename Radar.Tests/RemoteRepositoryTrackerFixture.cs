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

        [Fact]
        public void CanDetectABranchResetToAKnownCommit()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName, c3));

                var tracker = BuildSUT(repo);

                PerformUpstreamChange(r => ResetBranchToKnownCommit(r, branchName, c1));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchResetToAKnownCommit, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(c1.Sha, e.Shas.Single());
            }
        }

        [Fact]
        public void CanDetectADeletedBranch()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName, c3));

                var tracker = BuildSUT(repo);

                PerformUpstreamChange(r => DeleteBranch(r, branchName));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchDeleted, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Empty(e.Shas);
            }
        }

        [Fact]
        public void CanDetectACreatedBranch()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var tracker = BuildSUT(repo);

                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                var createdCommitsShas = PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName, c1));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsNotType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchCreated, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(createdCommitsShas, e.Shas);
            }
        }

        [Fact]
        public void CanDetectACreatedRootBranch()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var tracker = BuildSUT(repo);

                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                var createdCommitsShas = PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsNotType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchCreated, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(createdCommitsShas, e.Shas);
            }
        }

        [Fact]
        public void CanDetectAnUpdatedBranch()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName, c1));

                var tracker = BuildSUT(repo);

                var updatedCommitsShas = PerformUpstreamChange(r => UpdateBranchWithSomeCommits(r, branchName));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsNotType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchUpdated, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(updatedCommitsShas, e.Shas);
            }
        }

        [Fact]
        public void CanDetectAForceUpdatedBranch()
        {
            var path = CloneUpstream();

            using (var repo = new Repository(path))
            {
                var branchName = string.Format("branch-{0}", Guid.NewGuid());

                PerformUpstreamChange(r => CreateBranchWithSomeCommits(r, branchName, c1));

                var tracker = BuildSUT(repo);

                PerformUpstreamChange(r => ResetBranchToKnownCommit(r, branchName, c3));
                var updatedCommitsShas = PerformUpstreamChange(r => UpdateBranchWithSomeCommits(r, branchName));

                var evts = RetrieveActivity(tracker);

                Assert.Equal(1, evts.Count);
                Event e = evts.Single();

                Assert.IsNotType<NullIdentity>(e.Identity);
                Assert.Equal(EventKind.BranchForceUpdated, e.Kind);
                Assert.Equal(branchName, e.ShortReferenceName);
                Assert.Equal(updatedCommitsShas.Concat(new[] { c3.Sha, c2.Sha }), e.Shas);
            }
        }
        private void DeleteBranch(IRepository repo, string branchName)
        {
            var branch = repo.Branches[branchName];

            repo.Branches.Remove(branch);
        }

        private void ResetBranchToKnownCommit(IRepository repo, string branchName, Commit to)
        {
            var branchRef = repo.Refs[string.Format("refs/heads/{0}", branchName)];

            repo.Refs.UpdateTarget(branchRef, to.Id);
        }

        private string[] CreateBranchWithSomeCommits(IRepository repo, string branchName, Commit from = null)
        {
            return RandomCommitAdder(repo, r =>
            {
                if (from == null)
                {
                    repo.Refs.UpdateTarget("HEAD", string.Format("refs/heads/{0}", branchName));
                    repo.RemoveUntrackedFiles();
                }
                else
                {
                    var newBranch = repo.Branches.Add(branchName, from);
                    repo.Checkout(newBranch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                }
            });
        }

        private string[] UpdateBranchWithSomeCommits(IRepository repo, string branchName)
        {
            return RandomCommitAdder(repo, r =>
            {
                var branch = repo.Branches[branchName];
                repo.Checkout(branch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

            });
        }

        private string[] RandomCommitAdder(IRepository repo, Action<IRepository> repoModifier)
        {
            var currentHead = repo.Head;

            repoModifier(repo);

            var r1 = AddRandomCommit(repo);
            var r2 = AddRandomCommit(repo);
            var r3 = AddRandomCommit(repo);

            repo.Checkout(currentHead, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

            return new[] { r3.Sha, r2.Sha, r1.Sha };
        }

        private void CreateBranchFromAKnownCommit(IRepository repo, string branchName, Commit commit)
        {
            repo.Branches.Add(branchName, commit);
        }

        private void PerformUpstreamChange(Action<IRepository> stateModifier)
        {
            stateModifier(upstream);
        }

        private string[] PerformUpstreamChange(Func<IRepository, string[]> stateModifier)
        {
            return stateModifier(upstream);
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
