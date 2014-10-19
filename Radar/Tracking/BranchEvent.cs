namespace Radar.Tracking
{
    public class BranchEvent
    {
        private readonly string canonicalName;
        private readonly string name;
        private readonly string oldSha;
        private readonly string newSha;
        private readonly BranchEventKind kind;

        public BranchEvent(string canonicalName, string oldSha, string newSha, BranchEventKind kind)
        {
            this.canonicalName = canonicalName;
            name = CanonicalToShort(canonicalName);
            this.oldSha = oldSha;
            this.newSha = newSha;
            this.kind = kind;
        }

        public string CanonicalName
        {
            get { return canonicalName; }
        }

        public string Name
        {
            get { return name; }
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

        private static string CanonicalToShort(string canonicalName)
        {
            return canonicalName.Substring("refs/heads/".Length);
        }
    }
}
