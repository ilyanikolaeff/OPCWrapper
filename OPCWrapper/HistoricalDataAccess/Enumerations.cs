namespace OPCWrapper.HistoricalDataAccess
{
    public enum FindType
    {
        First,
        Last
    }

    public enum IntervalChangeType
    {
        Constriction,
        Extension,
        None
    }

    public enum FilterType
    {
        QualityGood,
        ValueNotNull,
        GoodAndNotNull
    }
}
