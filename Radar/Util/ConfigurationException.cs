using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radar.Util
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message)
            : base(String.Format("Configuration error: {0}", message))
        {
        }
    }
}
