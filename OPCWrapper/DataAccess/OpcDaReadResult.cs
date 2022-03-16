using Opc;
using Opc.Da;
using System;

namespace OPCWrapper.DataAccess
{
    public class OpcDaReadResult : OperationResultBase
    {
        private readonly ItemValueResult _itemValueResult;

        public override string ItemName => _itemValueResult.ItemName;

        public DateTime Timestamp => _itemValueResult.Timestamp;

        public object Value => _itemValueResult.Value;

        public override bool IsSuccess => _itemValueResult.ResultID == ResultID.S_OK;

        public int Quality => _itemValueResult.Quality.GetCode();

        public override string OperationResult => _itemValueResult.ResultID.ToString();

        public OpcDaReadResult(ItemValueResult itemValueResult)
        {
            _itemValueResult = itemValueResult;
        }
    }
}
