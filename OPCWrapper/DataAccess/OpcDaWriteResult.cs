using Opc;

namespace OPCWrapper.DataAccess
{
    public class OpcDaWriteResult : OperationResultBase
    {
        private readonly IdentifiedResult _identifiedResult;
        public OpcDaWriteResult(IdentifiedResult identifiedResult)
        {
            _identifiedResult = identifiedResult;
        }

        public override string ItemName => _identifiedResult.ItemName;

        public override bool IsSuccess => _identifiedResult.ResultID == ResultID.S_OK;

        public override string OperationResult => _identifiedResult.ResultID.ToString();

    }
}
