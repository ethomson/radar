using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Radar.Tracking;
using Radar.Util;

namespace Radar.Clients
{
    public class RepositoryClient : Client
    {
        private readonly RepositoryClientConfiguration configuration;
        private ITracer tracer;

        private readonly Object runningLock = new Object();
        private bool running;
        private IRepository repository;
        private RemoteRepositoryTracker tracker;

        public RepositoryClient(RepositoryClientConfiguration configuration)
        {
            this.configuration = configuration;
            this.tracer = new NullTracer();
        }

        public ClientConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

        public ITracer Tracer
        {
            get
            {
                return tracer;
            }

            set
            {
                tracer = value;
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
                    repository,
                    SnoozedRepositoriesRetriever, ForkedRepositoriesRetriever,
                    tracer);
            }
        }

        public IEnumerable<Event> RecentEvents()
        {
            var recentEvents = new List<Event>();

            foreach (var kvp in tracker.ProbeMonitoredRepositoriesState())
            {
                var monitoredRepository = kvp.Key;

                foreach (var branchEvent in kvp.Value)
                {
                    recentEvents.Add(new Event
                    {
                        Time = DateTime.Now,
                        Identity = new Identity{ Name = "Unknown", Email = "someone@somewhere.com"},
                        Content = string.Format("{0} branch {1} in repository {4}: old = {2} / new = {3}",
                            branchEvent.Kind, branchEvent.Name, branchEvent.OldSha, branchEvent.NewSha,
                            monitoredRepository.FriendlyName),
                    });
                    }
            }

            return recentEvents;
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

        private void branchEventsNotification(MonitoredRepository mr, BranchEvent[] events)
        {
            tracer.WriteInformation("Changes detected in {0} monitored repository '{1}'", mr.Origin, mr.FriendlyName);

            foreach (var branchEvent in events)
            {

            }
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
