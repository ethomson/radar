using System;
using System.Reflection;
using System.Threading;
using Radar.Util;

namespace Radar
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var tracer = new ConsoleTracer();

            try
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
                    configPath ?? Configuration.DefaultConfigurationPath);

                Radar radar = new Radar(config);

                radar.Tracer = tracer;
                radar.Start();

                tracer.WriteInformation("Sleeping for some minutes...");
                Thread.Sleep(TimeSpan.FromSeconds(60 * 5));

                radar.Stop();
            }
            catch(Exception e)
            {
                tracer.WriteError("{0}: {1}", ProgramName, e.Message);
            }

            return 0;
        }

        private static String ProgramName
        {
            get
            {
                int lastSlash;

                String programName = Assembly.GetExecutingAssembly().Location;

                if ((lastSlash = programName.LastIndexOf('\\')) >= 0)
                    programName = programName.Substring(lastSlash + 1);

                if (programName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    programName = programName.Substring(0, programName.Length - 4);

                return programName;
            }
        }
    }
}
