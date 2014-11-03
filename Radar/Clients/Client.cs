using System.Collections.Generic;
using Radar.Util;

namespace Radar.Clients
{
    public interface Client
    {
        ClientConfiguration Configuration { get; }
        bool Running { get; }
        string Name { get; }
        void Start();
        IEnumerable<IEvent> RecentEvents();
        void Stop();
    }
}
