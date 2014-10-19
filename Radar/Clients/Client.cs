using System.Collections.Generic;
using Radar.Util;

namespace Radar.Clients
{
    public interface Client
    {
        ClientConfiguration Configuration { get; }
        ITracer Tracer { get; set; }
        bool Running { get; }
        string Name { get; }
        void Start();
        IEnumerable<Event> RecentEvents();
        void Stop();
    }
}
