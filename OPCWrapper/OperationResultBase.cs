namespace OPCWrapper
{
    public abstract class OperationResultBase
    {
        public abstract string ItemName { get; }
        public abstract bool IsSuccess { get; }
        public abstract string OperationResult { get; }
    }
}
