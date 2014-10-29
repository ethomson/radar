using System;

namespace Radar
{
    public class Identity
    {
        public string Name { get; internal set; }
        public string Email { get; internal set; }
        public Uri Image { get; internal set; }
    }

    public class NullIdentity : Identity
    {
        public NullIdentity()
        {
            Name = "Unknown";
            Email = "dont@know.com";
        }
    }
}
