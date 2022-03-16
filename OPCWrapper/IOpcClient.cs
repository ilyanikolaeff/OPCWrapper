using NLog;

namespace OPCWrapper
{
    interface IOpcClient
    {
        string ClientName { get; set; }
        ConnectionSettings ConnectionSettings { get; set; }

        void RegisterLogger(ILogger logger);
        void UnregisterLogger();
    }
}
