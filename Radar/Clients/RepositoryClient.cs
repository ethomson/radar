using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using Radar.Tracking;
using Radar.Util;

namespace Radar.Clients
{
    public class RepositoryClient : Client
    {
        private readonly Radar radar;
        private readonly RepositoryClientConfiguration configuration;

        private readonly Object runningLock = new Object();
        private bool running;
        private IRepository repository;
        private RemoteRepositoryTracker tracker;

        public RepositoryClient(Radar radar, RepositoryClientConfiguration configuration)
        {
            Assert.NotNull(radar, "radar");
            Assert.NotNull(configuration, "configuration");

            this.radar = radar;
            this.configuration = configuration;
        }

        public ClientConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

        public bool Running
        {
            get
            {
                lock (runningLock)
                {
                    return running;
                }
            }
        }

        public string Name
        {
            get
            {
                string name = System.IO.Path.GetFileName(configuration.Path);

                if (name.EndsWith(".git"))
                {
                    name = name.Substring(0, name.Length - 4);
                }

                return name;
            }
        }

        public void Start()
        {
            lock (runningLock)
            {
                if (running)
                {
                    Stop();
                }

                running = true;

                repository = new Repository(configuration.Path);
                tracker = new RemoteRepositoryTracker(
                    radar,
                    repository,
                    SnoozedRepositoriesRetriever, ForkedRepositoriesRetriever);
            }
        }

        public IEnumerable<Event> RecentEvents()
        {
            var eventsRetrieval = (from mr in tracker.MonitoredRepositories
                                   select tracker.ProbeMonitoredRepositoriesState(mr)).ToArray();

            IEnumerable<Event>[] events = Task.WhenAll(eventsRetrieval).Result;

            return events.SelectMany(evs => evs);
        }

        public void Stop()
        {
            lock (runningLock)
            {
                running = false;
            }

            tracker = null;
            repository.Dispose();
            repository = null;
        }

        private ICollection<MonitoredRepository> SnoozedRepositoriesRetriever()
        {
            return configuration.Snoozed
                .Select(s => new MonitoredRepository(s, "snoozed", RepositoryOrigin.Unknown))
                .ToList();
        }

        private ICollection<MonitoredRepository> ForkedRepositoriesRetriever()
        {
            return configuration.Forks
                .Select(fc => new MonitoredRepository(fc.Url, fc.Name, RepositoryOrigin.Fork))
                .ToList();
        }

    }
}
