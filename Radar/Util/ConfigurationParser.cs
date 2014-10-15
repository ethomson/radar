using System;

using Newtonsoft.Json;

namespace Radar.Util
{
    public class ConfigurationParser
    {
        readonly dynamic configuration;

        public ConfigurationParser(string configJson)
        {
            configuration = JsonConvert.DeserializeObject(configJson);
        }

        public ConfigurationParser(dynamic configuration)
        {
            this.configuration = configuration;
        }

        public delegate void ExecuteCallback(dynamic value);

        public dynamic Get(string key)
        {
            if (configuration[key] == null)
            {
                throw new ConfigurationException(String.Format("required value '{0}' not found", key));
            }

            return configuration[key];
        }

        public dynamic TryGet(string key)
        {
            return configuration[key];
        }

        public void Execute(string key, ExecuteCallback cb)
        {
            if (configuration[key] == null)
            {
                throw new ConfigurationException(String.Format("required value '{0}' not found", key));
            }

            cb(configuration[key]);
        }

        public void TryExecute(string key, ExecuteCallback cb)
        {
            if (configuration[key] != null)
            {
                cb(configuration[key]);
            }
        }
    }
}
