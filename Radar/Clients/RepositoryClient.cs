using System;
using System.Collections.Generic;

namespace Radar.Clients
{
    public class RepositoryClient : Client
    {
        private readonly RepositoryClientConfiguration configuration;
        private ITracer tracer;

        private readonly Object runningLock = new Object();
        private bool running;

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
