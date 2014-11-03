using Radar.Tracking;
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
        public void TwoRepositoriesWithTheSameUrlAreEqual()
        {
            var mr = new MonitoredRepository("url", "name", RepositoryOrigin.Fork);
            var mr2 = new MonitoredRepository("url", "anotherName", RepositoryOrigin.Remote);

            Assert.Equal(mr, mr2);
        }

        [Fact]
        public void FriendlyNameCanBeInferedFromAGitHubUrl()
        {
            const string url = "https://github.com/nulltoken/libgit2sharp.git";
            var mr = new MonitoredRepository(url, RepositoryOrigin.Fork);

            Assert.Equal(url, mr.Url);
            Assert.Equal("nulltoken", mr.FriendlyName);
            Assert.Equal(RepositoryOrigin.Fork, mr.Origin);
        }
    }
}
