using System.Collections.Generic;

namespace Radar.Clients
{
    public interface Client
    {
        ClientConfiguration Configuration { get; }
        bool Running { get; }
        string Name { get; }
        void Start();
        IEnumerable<Event> RecentEvents();
        void Stop();
    }
}
