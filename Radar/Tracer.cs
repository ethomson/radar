using System;

namespace Radar
{
    public class Tracer : ITrace
    {
        public void WriteInformation(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
