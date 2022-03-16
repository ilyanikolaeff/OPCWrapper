using NLog;
using Opc.Hda;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OPCWrapper.HistoricalDataAccess
{
    public class OpcHdaClient : IDisposable, IOpcClient
    {
        private ILogger _logger;
        private Server _server;

        public string ClientName { get; set; }
        public ConnectionSettings ConnectionSettings { get; set; }

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (_server != null)
                    {
                        _logger?.Trace($"[{ClientName}] Cостояние подключения = [{_server.IsConnected}]");
                        return _server.IsConnected;
                    }
                    else
                    {
                        _logger?.Trace($"[{ClientName}] Cостояние подключения = [BAD]");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[{ClientName}] Не удалось запросить информацию о состоянии подключения. Сообщение об ошибке: {ex}");
                    return false;
                }
            }
        }

        public OpcHdaClient(ConnectionSettings connectionSettings, string clientName = "default")
        {
            ConnectionSettings = connectionSettings;
            ClientName = clientName;
        }
        public bool Connect()
        {
            try
            {
                var opcUrl = new Opc.URL($"opchda://{ConnectionSettings.IPAddress}/{ConnectionSettings.ServerName}");
                _logger?.Trace($"[{ClientName}] Попытка подключения к {opcUrl}");
                var opcFactory = new OpcCom.Factory();
                _server = new Server(opcFactory, opcUrl);
                _server.Connect();
                _logger?.Trace($"[{ClientName}] Подключен к {opcUrl}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка подключения к HDA серверу. Сообщение об ошибке: {ex}");
                return false;
            }
        }
        public bool Disconnect()
        {
            try
            {
                if (_server != null)
                {
                    _logger?.Trace($"[{ClientName}] Отключение от сервера [{_server.Url}]");
                    _server.Disconnect();
                    _logger?.Trace($"[{ClientName}] Отключение от сервера [{_server.Url}] успешно выполнено");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка отключения от HDA сервера. Сообщение об ошибке: {ex}");
                return false;
            }
        }
        public void Dispose()
        {
            Disconnect();
            UnregisterLogger();
        }
        public List<OpcHdaResultsCollection> ReadRaw(DateTime startTime, DateTime endTime, IEnumerable<string> tagNames, int maxValues = 0, bool includeBounds = true)
        {
            try
            {
                _logger?.Trace($"[{ClientName}] Чтение истории [{tagNames.Count()}] тегов c параметрами => " +
                    $"Start Time = [{startTime}], End Time = [{endTime}], Max values = [{maxValues}], Include bounds = [{includeBounds}]");
                var diagTimer = new Stopwatch();
                diagTimer.Start();

                var items = new Item[tagNames.Count()];
                int index = 0;
                foreach (var tagName in tagNames)
                {
                    items[index] = new Item(new Opc.ItemIdentifier(tagName));
                    index++;
                }

                var identifiedResultsArray = _server.CreateItems(items);
                var historyRawValues = _server.ReadRaw(new Time(startTime), new Time(endTime), maxValues, includeBounds, identifiedResultsArray);

                var results = new List<OpcHdaResultsCollection>();
                foreach (var historyValuesCollection in historyRawValues)
                {
                    results.Add(new OpcHdaResultsCollection(historyValuesCollection));
                }
                diagTimer.Stop();
                _logger?.Trace($"[{ClientName}] Чтение истории [{tagNames.Count()}] тегов выполнено за [{diagTimer.Elapsed}]");

                return results;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Чтение истории завершилось с ошибкой. Сообщение об ошибке: {ex}");
                return null;
            }
        }

        public void RegisterLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void UnregisterLogger()
        {
            _logger = null;
        }
    }
}
