using Xunit;

namespace Radar.Tests
{
    public class MonitoredRepositoryFixture
    {
        [Fact]
        public void PropertiesExposeCtorParameters()
        {
            var mr = new MonitoredRepository("url", "name", RepositoryOrigin.Fork);

            Assert.Equal("url", mr.Url);
            Assert.Equal("name", mr.FriendlyName);
            Assert.Equal(RepositoryOrigin.Fork, mr.Origin);
        }

        [Fact]
        public void OriginAndUrlAndNameCanBeInferedFromAGitHubUrl()
        {
            const string url = "https://github.com/nulltoken/libgit2sharp.git";
            var mr = new MonitoredRepository(url);

            Assert.Equal(url, mr.Url);
            Assert.Equal("nulltoken", mr.FriendlyName);
            Assert.Equal(RepositoryOrigin.Remote, mr.Origin);
        }
    }
}
