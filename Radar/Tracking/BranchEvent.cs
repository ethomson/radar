using System;
using System.Linq;
using Radar.Util;

namespace Radar.Tracking
{
    public class BranchEvent
    {
        private readonly string canonicalName;
        private readonly string oldSha;
        private readonly string newSha;
        private readonly BranchEventKind kind;
        private readonly Func<string, Tuple<Identity, DateTime>> commitSignatureRetriever;
        private bool isFullyAnalyzed;
        private string[] shas = { };
        private readonly Event ev = new Event();

        public BranchEvent(
            string canonicalName, string oldSha, string newSha,
            BranchEventKind kind, Func<string, Tuple<Identity, DateTime>> commitSignatureRetriever)
        {
            this.canonicalName = canonicalName;
            this.oldSha = oldSha;
            this.newSha = newSha;
            this.kind = kind;
            this.commitSignatureRetriever = commitSignatureRetriever;
            ev.Kind = EventKind.PendingAnalysis;
            ev.BranchName = CanonicalName;

            if (kind == BranchEventKind.Deleted)
            {
                MarkAsDeletedBranch();
            }
        }

        public bool IsFullyAnalyzed
        {
            get { return isFullyAnalyzed; }
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

            shas = new[] { NewSha };

            ev.Kind = EventKind.BranchCreatedFromKnownCommit;
            SetEventSignatureToUnknown();
        }

        public void MarkAsResetBranchToAKnownCommit()
        {
            MarkFullyAnalyzed();

            shas = new[] { NewSha };

            ev.Kind = EventKind.BranchResetToAKnownCommit;
            SetEventSignatureToUnknown();
        }

        private void MarkAsDeletedBranch()
        {
            MarkFullyAnalyzed();

            ev.Kind = EventKind.BranchDeleted;
            SetEventSignatureToUnknown();
        }

        public void MarkAsUpdatedBranchWithNewCommits(bool isForcePushed, string[] newShas)
        {
            MarkFullyAnalyzed();

            shas = newShas;

            ev.Kind = isForcePushed ? EventKind.BranchForceUpdated :
                (Kind == BranchEventKind.Created ? EventKind.BranchCreated : EventKind.BranchUpdated);

            FillEventSignature();
        }

        private void MarkFullyAnalyzed()
        {
            Assert.IsTrue(!isFullyAnalyzed, "!isFullyAnalyzed");

            isFullyAnalyzed = true;
        }

        private void SetEventSignatureToUnknown()
        {
            ev.Identity = new NullIdentity();
            ev.Time = DateTime.Now;
        }

        private void FillEventSignature()
        {
            // TODO: Instead of retrieving the identity of the last committer
            // we should rather publish one event per committer

            var sign = commitSignatureRetriever(shas.Last());

            ev.Identity = sign.Item1;
            ev.Time = sign.Item2;
        }

        public Event BuildEvent(MonitoredRepository mr)
        {
            Assert.IsTrue(isFullyAnalyzed, "isFullyAnalyzed");
            Assert.IsTrue(ev.Kind != EventKind.PendingAnalysis, "EventKind != EventKind.PendingAnalysis");
            Assert.IsTrue(ev.Identity != null, "ev.Identity != null");
            Assert.IsTrue(ev.Time != DateTime.MinValue, "ev.Time != DateTime.MinValue");

            ev.RepositoryFriendlyName = mr.FriendlyName;
            ev.Shas = shas;

            return ev;
        }
    }
}
