using System;

namespace Radar
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string configPath = null;

            foreach (string arg in args)
            {
                if (arg.StartsWith("/config:") || arg.StartsWith("/config="))
                {
                    configPath = arg.Substring(8);
                    continue;
                }

                throw new Exception(String.Format("Invalid argument: {0}", arg));
            }

            Configuration config = Configuration.LoadFrom(
                (configPath != null) ? configPath : Configuration.DefaultConfigurationPath);

            Radar radar = new Radar(config);

            radar.Start();
            radar.Stop();

            return 0;
        }
    }
}
