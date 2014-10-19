using System;
using System.Collections.Generic;

namespace Radar.Clients
{
    public class RepositoryClient : Client
    {
        private readonly RepositoryClientConfiguration configuration;

        private readonly Object runningLock = new Object();
        private bool running;

        public RepositoryClient(RepositoryClientConfiguration configuration)
        {
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
            }
        }

        public IEnumerable<Event> RecentEvents()
        {
            return new List<Event>();
        }

        public void Stop()
        {
            lock (runningLock)
            {
                running = false;
            }
        }
    }
}
