namespace Radar
{
    public class BranchEvent
    {
        private readonly string _canonicalName;
        private readonly string _name;
        private readonly string _oldSha;
        private readonly string _newSha;
        private readonly BranchEventKind _kind;

        public BranchEvent(string canonicalName, string oldSha, string newSha, BranchEventKind kind)
        {
            _canonicalName = canonicalName;
            _name = CanonicalToShort(canonicalName);
            _oldSha = oldSha;
            _newSha = newSha;
            _kind = kind;
        }

        public string CanonicalName
        {
            get { return _canonicalName; }
        }

        public string Name
        {
            get { return _name; }
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

        private static string CanonicalToShort(string canonicalName)
        {
            return canonicalName.Substring("refs/heads/".Length);
        }
    }
}
