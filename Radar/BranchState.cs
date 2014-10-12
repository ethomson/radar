namespace Radar
{
    internal class BranchState
    {
        private readonly string _branchName;
        private string _sha;

        public BranchState(string branchName)
        {
            _branchName = branchName;
        }

        public string BranchName
        {
            get { return _branchName; }
        }

        public string Sha
        {
            get { return _sha; }
        }

        public void UpdateTip(string sha)
        {
            _sha = sha;
        }
    }
}
