namespace Server.Custom.AIGM
{
    public interface IUMGMovementExecutor
    {
        UMGMovementExecutionResult Execute(UMGMovementExecutionRequest request);
    }
}
