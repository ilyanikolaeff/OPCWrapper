using Opc.Hda;
using System;

namespace OPCWrapper.HistoricalDataAccess
{
    public class OpcHdaResultItem
    {
        private readonly ItemValue _itemValue;
        public OpcHdaResultItem(ItemValue itemValue)
        {
            _itemValue = itemValue;
        }

        public int Quality => _itemValue.Quality.GetCode();
        public DateTime Timestamp => _itemValue.Timestamp;
        public object Value => _itemValue.Value;

        public bool Compare(OpcHdaResultItem other)
        {
            return (Value.ToString() == other.Value.ToString()) && (Timestamp == other.Timestamp) && (Quality == other.Quality);
        }
    }
}
