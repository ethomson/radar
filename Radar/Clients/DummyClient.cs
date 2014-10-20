using System;
using System.Collections.Generic;

using Radar.Util;

namespace Radar.Clients
{
    public class DummyClient : Client
    {
        private readonly DummyClientConfiguration configuration;

        private readonly Object runningLock = new Object();
        private volatile bool running;

        public DummyClient(DummyClientConfiguration configuration)
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
                return running;
            }
        }

        public ITracer Tracer { get; set; }

        public string Name
        {
            get
            {
                return "dummy";
            }
        }

        public void Start()
        {
            running = true;
        }

        public IEnumerable<Event> RecentEvents()
        {
            List<Event> events = new List<Event>();
            events.Add(new Event
            {
                Time = DateTime.Now,
                Identity = new Identity { Name = "Test User", Email = "test@test.test" },
                Content = "This is a test event",
            });
            return events;
        }

        public void Stop()
        {
            running = false;
        }
    }
}