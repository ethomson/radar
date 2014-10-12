namespace Radar
{
    public class BranchEvent
    {
        private readonly string _branchName;
        private readonly string _tipSha;
        private readonly BranchEventKind _kind;

        public BranchEvent(string branchName, string tipSha, BranchEventKind kind)
        {
            _branchName = branchName;
            _tipSha = tipSha;
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

        public string TipSha
        {
            get { return _tipSha; }
        }
    }
}
