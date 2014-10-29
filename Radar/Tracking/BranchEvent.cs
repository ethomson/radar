using Radar.Util;

namespace Radar.Tracking
{
    public class BranchEvent
    {
        private readonly string canonicalName;
        private readonly string oldSha;
        private readonly string newSha;
        private readonly BranchEventKind kind;
        private bool isFullyAnalyzed;
        private EventKind _eventKind;
        private string[] shas = { };

        public BranchEvent(string canonicalName, string oldSha, string newSha, BranchEventKind kind)
        {
            this.canonicalName = canonicalName;
            this.oldSha = oldSha;
            this.newSha = newSha;
            this.kind = kind;

            if (kind == BranchEventKind.Deleted)
            {
                MarkAsDeletedBranch();
            }
        }

        public bool IsFullyAnalyzed
        {
            get { return isFullyAnalyzed; }
        }

        public EventKind EventKind
        {
            get { return _eventKind; }
        }

        public string[] Shas
        {
            get { return shas; }
        }

        public string CanonicalName
        {
            get { return canonicalName; }
        }

        public BranchEventKind Kind
        {
            get { return kind; }
        }

        public string OldSha
        {
            get { return oldSha; }
        }

        public string NewSha
        {
            get { return newSha; }
        }

        public void MarkAsNewBranchFromKnownCommit()
        {
            MarkFullyAnalyzed();

            _eventKind = EventKind.BranchCreatedFromKnownCommit;
            shas = new[] { NewSha };
        }

        public void MarkAsResetBranchToAKnownCommit()
        {
            MarkFullyAnalyzed();

            _eventKind = EventKind.BranchResetToAKnownCommit;
            shas = new[] { NewSha };
        }

        private void MarkAsDeletedBranch()
        {
            MarkFullyAnalyzed();

            _eventKind = EventKind.BranchDeleted;
        }

        public void MarkAsUpdatedBranchWithNewCommits(bool isForcePushed, string[] newShas)
        {
            MarkFullyAnalyzed();

            _eventKind = isForcePushed ? EventKind.BranchForceUpdated :
                (Kind == BranchEventKind.Created ? EventKind.BranchCreated : EventKind.BranchUpdated);

            shas = newShas;
        }

        private void MarkFullyAnalyzed()
        {
            Assert.IsTrue(!isFullyAnalyzed, "!isFullyAnalyzed");

            isFullyAnalyzed = true;
        }

        public Event BuildEvent(MonitoredRepository mr)
        {
            Assert.IsTrue(isFullyAnalyzed, "isFullyAnalyzed");
            Assert.IsTrue(EventKind != EventKind.PendingAnalysis, "EventKind != EventKind.PendingAnalysis");

            return new Event
            {
                RepositoryFriendlyName = mr.FriendlyName,
                BranchName = CanonicalName,
                Kind = EventKind,
                Shas = shas
            };
        }
    }
}
