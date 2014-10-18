using System;
using System.Threading;
using LibGit2Sharp;

namespace Radar
{
    class Program
    {
        static void Main(string[] args)
        {
            var tracer = new Tracer();
            tracer.WriteInformation("Launching...");


            using (var repository = new Repository(@"D:\Radar.TestRepo"))
            using (var tracker = new RemoteRepositoryTracker(
                repository,
                SnoozedRepositoriesRetriever, ForkedRepositoriesRetriever,
                forkEventsNotification, branchEventsNotification,
                tracer))
            {
                tracker.StartTracking(TrackingType.BranchMonitoring, TimeSpan.FromSeconds(1));

                tracer.WriteInformation("Sleeping...");
                Thread.Sleep(TimeSpan.FromSeconds(60*5));
                tracer.WriteInformation("Disposing...");
            }

            Console.ReadLine();
        }

        private static void branchEventsNotification(MonitoredRepository monitoredRepository, BranchEvent[] events)
        {
            Console.WriteLine("Changes detected in {0} monitored repository '{1}'", monitoredRepository.Origin, monitoredRepository.FriendlyName);
            foreach (var branchEvent in events)
            {
                Console.WriteLine("{0} {1}: old = {2} / new = {3}", branchEvent.Kind, branchEvent.BranchName, branchEvent.OldSha, branchEvent.NewSha);
            }
        }

        private static void forkEventsNotification(ForkEvent[] obj)
        {
            throw new NotImplementedException();
        }

        private static MonitoredRepository[] ForkedRepositoriesRetriever()
        {
            var remotes = new MonitoredRepository[]
            {
                new MonitoredRepository("https://github.com/arthurschreiber/radar.testrepo.git", RepositoryOrigin.Fork),
            };

            // Bonus pack (maybe) : Dynamically retrieve the network of forks through Octokit
            // See https://github.com/octokit/octokit.net/pull/495
            //
            // Although this is not merged yet, we may be able to go low level and dynamically retrieve all those.

            return remotes;
        }

        private static MonitoredRepository[] SnoozedRepositoriesRetriever()
        {
            var remotes = new[]
            {
                new MonitoredRepository("https://github.com/trollface/radar.testrepo.git", RepositoryOrigin.Unknown),
            };

            return remotes;
        }
    }
}
