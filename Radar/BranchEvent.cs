namespace Radar
{
    public class BranchEvent
    {
        private readonly string _branchName;
        private readonly string _oldSha;
        private readonly string _newSha;
        private readonly BranchEventKind _kind;

        public BranchEvent(string branchName, string oldSha, string newSha, BranchEventKind kind)
        {
            _branchName = branchName;
            _oldSha = oldSha;
            _newSha = newSha;
            _kind = kind;
        }

        public string BranchName
        {
            get { return _branchName; }
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
