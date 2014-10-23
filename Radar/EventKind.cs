namespace Radar
{
    public enum EventKind
    {
        PendingAnalysis = 0,
        BranchCreated,
        BranchCreatedFromKnownCommit,
        BranchResetToAKnownCommit,
        BranchUpdated,
        BranchForceUpdated,
        BranchDeleted,
    }
}
