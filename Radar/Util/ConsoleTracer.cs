using System;

namespace Radar.Util
{
    public class ConsoleTracer : ITracer
    {
        public void WriteInformation(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            Console.Error.WriteLine(format, args);
        }
    }
}
