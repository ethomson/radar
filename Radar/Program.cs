using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Radar
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace("Launching...");

            ConcurrentBag<MonitoredRepository> repositories = RetrieveRepositoriesToTrack();

            Trace("Monitoring {0} repositories...", repositories.Count);

            var checkDelay = TimeSpan.FromSeconds(1);

            TrackNewRepositories(repositories, checkDelay);

            TrackChangesThatHaveOccuredInRemoteRepositories(repositories, checkDelay);
        }

        private static void TrackChangesThatHaveOccuredInRemoteRepositories(ConcurrentBag<MonitoredRepository> repositories, TimeSpan checkDelay)
        {
            var i = 0;

            var state = new ConcurrentDictionary<string, List<BranchState>>();

            while (true)
            {
                Task[] taskedUpdates = repositories
                                        .Select(remoteRepository => Task.Run(() => RefreshStateFromRemote(remoteRepository, state)))
                                        .ToArray();

                Task.WaitAll(taskedUpdates);

                Console.WriteLine(i++);
                Thread.Sleep(checkDelay);
            }
        }

        private static void TrackNewRepositories(ConcurrentBag<MonitoredRepository> known, TimeSpan checkDelay)
        {
            while (true)
            {
                var current = RetrieveRepositoriesToTrack();
                var newlyDiscovered = current.Except(known).ToList();

                var count = newlyDiscovered.Count();
                if (count != 0)
                {
                    foreach (var addedRemote in newlyDiscovered)
                    {
                        known.Add(addedRemote);
                    }

                    Trace("{0} new repositories are tracked", count);
                }
                else
                {
                    Trace("No new repositories have been detected");
                }

                Thread.Sleep(checkDelay);
            }
        }


        private static void RefreshStateFromRemote(MonitoredRepository repository, ConcurrentDictionary<string, List<BranchState>> state)
        {
            Trace("Probing repository '{0}'", repository);
        }

        private static void Trace(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        private static ConcurrentBag<MonitoredRepository> RetrieveRepositoriesToTrack()
        {
            var known = KnownRemotes();
            var forks = ForkedRepositories();

            var unknwonForks = forks.Except(known);
            
            var snoozed = SnoozedRepositories();

            var monitoredRepositories = known.Union(unknwonForks).Except(snoozed).Distinct();
            return new ConcurrentBag<MonitoredRepository>(monitoredRepositories);
        }

        private static MonitoredRepository[] KnownRemotes()
        {
            // Read the real remotes from the git repository
            var remotes = new MonitoredRepository[] { 
                "https://github.com/libgit2/libgit2sharp",  
                "https://github.com/nulltoken/libgit2sharp",  
                "https://github.com/ethomson/libgit2sharp",  
                "https://github.com/trollface/libgit2sharp",  
        };

            return remotes;
        }

        private static MonitoredRepository[] ForkedRepositories()
        {
            var remotes = new[]
            {
                new MonitoredRepository("https://github.com/nulltoken/libgit2sharp", RepositoryOrigin.Fork),
                new MonitoredRepository("https://github.com/ethomson/libgit2sharp", RepositoryOrigin.Fork),
                new MonitoredRepository("https://github.com/fork-2/libgit2sharp", RepositoryOrigin.Fork),
                new MonitoredRepository("https://github.com/fork-1/libgit2sharp", RepositoryOrigin.Fork),
            };

            // Bonus pack (maybe) : Dynamically retrieve the network of forks through Octokit
            // See https://github.com/octokit/octokit.net/pull/495
            //
            // Although this is not merged yet, we may be able to go low level and dynamically retrieve all those.

            return remotes;
        }

        private static MonitoredRepository[] SnoozedRepositories()
        {
            var remotes = new[]
            {
                new MonitoredRepository("https://github.com/trollface/libgit2sharp", RepositoryOrigin.Unknown),  
            };

            return remotes;
        }
    }
}
