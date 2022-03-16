using Opc;
using Opc.Hda;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OPCWrapper.HistoricalDataAccess
{
    public class OpcHdaResultsCollection : OperationResultBase, IEnumerable<OpcHdaResultItem>
    {
        private List<OpcHdaResultItem> _results { get; set; }
        public IEnumerable<OpcHdaResultItem> GetHistoryResults() => _results;
        public IEnumerator GetEnumerator()
        {
            return _results.GetEnumerator();
        }
        IEnumerator<OpcHdaResultItem> IEnumerable<OpcHdaResultItem>.GetEnumerator()
        {
            return _results.GetEnumerator();
        }
        public int Count { get => _results.Count(); }
        public DateTime EndTime { get; private set; }
        public DateTime StartTime { get; private set; }

        private ResultID _resultId;
        private string _itemName;
        public override string ItemName => _itemName;
        public override string OperationResult => _resultId.ToString();

        public override bool IsSuccess => _resultId == ResultID.S_OK;



        public OpcHdaResultsCollection(ItemValueCollection itemValueCollection)
        {
            _results = new List<OpcHdaResultItem>();
            _resultId = itemValueCollection.ResultID;
            _itemName = itemValueCollection.ItemName;

            EndTime = itemValueCollection.EndTime;
            StartTime = itemValueCollection.StartTime;

            foreach (ItemValue itemValue in itemValueCollection)
                _results.Add(new OpcHdaResultItem(itemValue));
        }
    }
}
