using System;

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
