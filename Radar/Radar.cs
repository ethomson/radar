using System;
using System.Collections.Generic;
using System.Threading;

using Radar.Clients;
using Radar.Notifications;
using Radar.Util;

namespace Radar
{
    public class Radar
    {
        private ITracer tracer;
        private readonly Object runningLock = new Object();
        private bool running;
        private DateTime? startTime;

        private readonly List<Client> clients = new List<Client>();
        private readonly List<Notification> notifications = new List<Notification>();

        public Radar(Configuration config)
        {
            Assert.NotNull(config, "config");

            Configuration = config;
            this.tracer = new NullTracer();
        }

        public Configuration Configuration
        {
            get;
            private set;
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

        public DateTime? StartTime
        {
            get
            {
                lock (runningLock)
                {
                    return startTime;
                }
            }
        }

        public void Start()
        {
            Assert.IsTrue(!Running, "!Running");

            foreach (ClientConfiguration clientConfig in Configuration.Clients)
            {
                Client client = ClientFactory.NewClient(clientConfig);

                if (client == null)
                {
                    tracer.WriteError("Could not find notification for type {0}", clientConfig.Type);
                    Environment.Exit(1);
                }

                client.Tracer = tracer;
                client.Start();
                clients.Add(client);
            }

            foreach (NotificationConfiguration notificationConfig in Configuration.Notifications)
            {
                Notification notification = NotificationFactory.NewNotification(notificationConfig);

                if (notification == null)
                {
                    tracer.WriteError("Could not find notification for type {0}", notificationConfig.Type);
                    Environment.Exit(1);
                }

                notifications.Add(NotificationFactory.NewNotification(notificationConfig));
            }

            lock (runningLock)
            {
                this.running = true;
                this.startTime = DateTime.Now;
            }

            while (Running)
            {
                foreach (Client client in clients)
                {
                    IEnumerable<Event> events;

                    try
                    {
                        events = client.RecentEvents();
                    }
                    catch (Exception e)
                    {
                        tracer.WriteError("Could not receive messages from {0}: {1} (ignoring)",
                            client.Configuration.Type, e.Message);
                        continue;
                    }

                    foreach (Event eventData in events)
                    {
                        foreach (Notification notification in notifications)
                        {
                            try
                            {
                                notification.Notify(client, eventData);
                            }
                            catch (Exception e)
                            {
                                tracer.WriteError("Could not notify using {0}: {1} (ignoring)",
                                    notification.Configuration.Type, e.Message);
                                continue;
                            }
                        }
                    }
                }

                Thread.Sleep(Configuration.PollInterval * 1000);
            }

            foreach (Client client in clients)
            {
                client.Stop();
            }

            lock (runningLock)
            {
                running = false;
                startTime = null;
            }
        }

        public void Stop()
        {
            lock (runningLock)
            {
                foreach (Notification notification in notifications)
                {
                    notification.Stop();
                }

                notifications.Clear();

                running = false;
            }
        }
    }
}
