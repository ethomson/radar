namespace Radar
{
    public class BranchEvent
    {
        private readonly string _canonicalName;
        private readonly string _oldSha;
        private readonly string _newSha;
        private readonly BranchEventKind _kind;

        public BranchEvent(string canonicalName, string oldSha, string newSha, BranchEventKind kind)
        {
            _canonicalName = canonicalName;
            _oldSha = oldSha;
            _newSha = newSha;
            _kind = kind;
        }

        public string CanonicalName
        {
            get { return _canonicalName; }
        }

        public BranchEventKind Kind
        {
            get { return _kind; }
        }

        public string OldSha
        {
            get { return _oldSha; }
        }

        public string NewSha
        {
            get { return _newSha; }
        }
    }
}
