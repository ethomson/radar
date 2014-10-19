using System;
using System.Reflection;

namespace Radar
{
    public class Program
    {
        public static int Main(string[] args)
        {
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

                radar.Start();
                radar.Stop();
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("{0}: {1}", ProgramName, e.Message);
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
