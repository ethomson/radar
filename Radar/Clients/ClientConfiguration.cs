using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radar.Clients
{
    public interface ClientConfiguration
    {
        string Type { get; }
        ClientConfiguration Duplicate();
    }
}
