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
        private EventKind refinedEventKind;
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
            refinedEventKind = EventKind.PendingAnalysis;

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

            refinedEventKind = EventKind.BranchCreatedFromKnownCommit;
            SetEventSignatureToUnknown();
        }

        public void MarkAsResetBranchToAKnownCommit()
        {
            MarkFullyAnalyzed();

            shas = new[] { NewSha };

            refinedEventKind = EventKind.BranchResetToAKnownCommit;
            SetEventSignatureToUnknown();
        }

        private void MarkAsDeletedBranch()
        {
            MarkFullyAnalyzed();

            refinedEventKind = EventKind.BranchDeleted;
            SetEventSignatureToUnknown();
        }

        public void MarkAsUpdatedBranchWithNewCommits(bool isForcePushed, string[] newShas)
        {
            MarkFullyAnalyzed();

            shas = newShas;

            refinedEventKind = isForcePushed ? EventKind.BranchForceUpdated :
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

        public IEvent BuildEvent(MonitoredRepository mr)
        {
            Assert.IsTrue(isFullyAnalyzed, "isFullyAnalyzed");
            Assert.IsTrue(refinedEventKind != EventKind.PendingAnalysis, "refinedEventKind != EventKind.PendingAnalysis");
            Assert.IsTrue(ev.Identity != null, "ev.Identity != null");
            Assert.IsTrue(ev.Time != DateTime.MinValue, "ev.Time != DateTime.MinValue");

            switch (refinedEventKind)
            {
                    case EventKind.BranchCreatedFromKnownCommit:
                        ev.Content = string.Format("In remote repository '{0}', a new branch '{1}' has been created from known commit [{2}]",
                            mr.FriendlyName, ToFriendlyName(CanonicalName), shas.Last());
                        break;
                    case EventKind.BranchResetToAKnownCommit:
                        ev.Content = string.Format("In remote repository '{0}', branch '{1}' has been reset to a known commit [{2}]",
                            mr.FriendlyName, ToFriendlyName(CanonicalName), shas.Last());
                        break;
                    case EventKind.BranchDeleted:
                        ev.Content = string.Format("In remote repository '{0}', branch '{1}' has been deleted",
                            mr.FriendlyName, ToFriendlyName(CanonicalName));
                        break;
                    case EventKind.BranchCreated:
                        ev.Content = string.Format("In remote repository '{0}', branch '{1}' has been created with new commits [{2}]",
                            mr.FriendlyName, ToFriendlyName(CanonicalName), string.Join(", ", shas));
                        break;
                    case EventKind.BranchUpdated:
                        ev.Content = string.Format("In remote repository '{0}', branch '{1}' has been updated with new commits [{2}]",
                            mr.FriendlyName, ToFriendlyName(CanonicalName), string.Join(", ", shas));
                        break;
                    case EventKind.BranchForceUpdated:
                        ev.Content = string.Format("In remote repository '{0}', branch '{1}' has been force updated with new commits [{2}]",
                            mr.FriendlyName, ToFriendlyName(CanonicalName), string.Join(", ", shas));
                        break;
            }

            return ev;
        }

        private string ToFriendlyName(string canonicalBranchName)
        {
            return canonicalBranchName.Substring("refs/heads/".Length);
        }
    }
}
