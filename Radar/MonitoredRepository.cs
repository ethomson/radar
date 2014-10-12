using System;
using Radar.LibGit2Sharp.Core;

namespace Radar
{
    public class MonitoredRepository : IEquatable<MonitoredRepository>
    {
        private readonly string _url;
        private readonly string _friendlyName;
        private readonly RepositoryOrigin _origin;

        private static readonly LambdaEqualityHelper<MonitoredRepository> equalityHelper =
            new LambdaEqualityHelper<MonitoredRepository>(x => x.Url);

        public MonitoredRepository(string url)
            : this(url, FriendlyNameFrom(url), RepositoryOrigin.Remote)
        {

        }

        private static string FriendlyNameFrom(string url)
        {
            const string githubCom = "https://github.com/";
            if (url.StartsWith(githubCom))
            {
                var nextForwardSlash = url.IndexOf('/', githubCom.Length);

                if (nextForwardSlash < 0)
                {
                    throw new InvalidOperationException(string.Format("Cannot extract friendly name from url {0}.", url));
                }
                var name = url.Substring(githubCom.Length, nextForwardSlash - githubCom.Length);

                return name;
            }

            throw new NotSupportedException();
        }

        public MonitoredRepository(string url, string friendlyName)
            : this(url, friendlyName, RepositoryOrigin.Remote)
        {
        }

        public MonitoredRepository(string url, RepositoryOrigin origin)
            : this(url, FriendlyNameFrom(url), origin)
        {
        }

        public MonitoredRepository(string url, string friendlyName, RepositoryOrigin origin)
        {
            _url = url;
            _friendlyName = friendlyName;
            _origin = origin;
        }

        public string Url
        {
            get { return _url; }
        }

        public string FriendlyName
        {
            get { return _friendlyName; }
        }

        public RepositoryOrigin Origin
        {
            get { return _origin; }
        }

        public static implicit operator MonitoredRepository(string url)
        {
            return new MonitoredRepository(url);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MonitoredRepository);
        }

        public bool Equals(MonitoredRepository other)
        {
            return equalityHelper.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        public static bool operator ==(MonitoredRepository left, MonitoredRepository right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MonitoredRepository left, MonitoredRepository right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return Url;
        }
    }
}
