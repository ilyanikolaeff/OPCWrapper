using Opc.Da;
using System.Collections.Generic;

namespace OPCWrapper.DataAccess
{
    class DataChangedEventHandlerWrapper
    {
        private OpcDaItemsChangedHandler _OpcItemsChanged;
        public void OnDataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] itemValueResults)
        {
            var opcItems = new List<OpcDaReadResult>();
            foreach (var itemValueResult in itemValueResults)
                opcItems.Add(new OpcDaReadResult(itemValueResult));
            _OpcItemsChanged?.Invoke(opcItems);
        }

        public DataChangedEventHandlerWrapper(OpcDaItemsChangedHandler opcDaItemChangedHandler)
        {
            _OpcItemsChanged = opcDaItemChangedHandler;
        }
    }

    public delegate void OpcDaItemsChangedHandler(IEnumerable<OpcDaReadResult> results);
}
