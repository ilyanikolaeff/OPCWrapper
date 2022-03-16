namespace OPCWrapper
{
    public class ConnectionSettings
    {
        private string _ipAddress;
        public string IPAddress
        {
            get => _ipAddress.ToLower().Equals("localhost") ? "127.0.0.1" : _ipAddress;
            private set => _ipAddress = value;
        }
        public string ServerName { get; private set; }

        public ConnectionSettings(string ipAddress, string serverName)
        {
            IPAddress = ipAddress;
            ServerName = serverName;
        }
    }
}
