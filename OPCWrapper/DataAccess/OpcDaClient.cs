using NLog;
using Opc;
using Opc.Da;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Server = Opc.Da.Server;

namespace OPCWrapper.DataAccess
{
    public class OpcDaClient : IDisposable, IOpcClient
    {
        private ILogger _logger;
        private Server _server;
        private readonly int _maxOperationItemsCount;
        public string ClientName { get; set; }
        public ConnectionSettings ConnectionSettings { get; set; }
        public OpcDaClient(ConnectionSettings connectionSettings, string clientName = "default", int maxOperationItemsCount = 5000)
        {
            ConnectionSettings = connectionSettings;
            ClientName = clientName;
            _maxOperationItemsCount = maxOperationItemsCount;
        }
        public bool Connect()
        {
            try
            {
                var opcUrl = new URL("opcda://" + ConnectionSettings.IPAddress + "/" + ConnectionSettings.ServerName);
                _logger?.Trace($"[{ClientName}] Попытка подключения к [{opcUrl}]");
                _server = new Server(new OpcCom.Factory(), opcUrl);
                _server.Connect();
                _logger?.Trace($"[{ClientName}] Подключен к [{opcUrl}]");
                return true; // connect good
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка подключения. Сообщение об ошибке: {ex}");
                return false; // connect bad
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
                _logger?.Error($"[{ClientName}] Ошибка отключения от сервера. Сообщение об ошибке: {ex}");
                return false;
            }

        }
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
        public void Subscribe(IEnumerable<string> tags, string groupName, int updateRate, OpcDaItemsChangedHandler opcDaItemChangedHandler)
        {
            try
            {
                if (_server.IsConnected)
                {
                    _logger?.Debug($"[{ClientName}] Подписка на [{tags.Count()}] сигнал(ов), название группы = [{groupName}], период опроса = [{updateRate}] мсек");
                    SubscriptionState subscriptionState = new SubscriptionState
                    {
                        Name = groupName,
                        UpdateRate = updateRate,
                        Active = true,
                    };
                    var subscriptionGroup = (Subscription)_server.CreateSubscription(subscriptionState);

                    // Add items
                    var opcDaItemsCollection = new List<Item>();
                    foreach (var tag in tags)
                    {
                        //_logger?.Debug($"[{ClientName}] Группа [{groupName}], подписка на сигнал [{tag}]");
                        opcDaItemsCollection.Add(new Item(new ItemIdentifier(tag)));
                    }

                    var opcDaItemsResultsArray = subscriptionGroup.AddItems(opcDaItemsCollection.ToArray());

                    var wrapper = new DataChangedEventHandlerWrapper(opcDaItemChangedHandler);
                    subscriptionGroup.DataChanged += wrapper.OnDataChanged;
                }
                else
                {
                    _logger?.Debug($"[{ClientName}] Подписка на сигналы не выполнена из-за отключенного состояние сервера");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Подписка на сигналы не выполнена из-за ошибки. Сообщение об ошибке: {ex}");
            }
        }
        public void Unsubscribe(string groupName)
        {
            try
            {
                _logger?.Debug($"[{ClientName}] Отмена подписки группы = [{groupName}]");
                if (_server != null)
                    foreach (Subscription subscriptionGroup in _server.Subscriptions)
                    {
                        if (subscriptionGroup.Name == groupName)
                        {
                            _server.CancelSubscription(subscriptionGroup);
                        }
                    }
                _logger?.Debug($"[{ClientName}] Отмена подписки группы = [{groupName}] выполнена успешно");
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Отмена подписки группы = [{groupName}] не выполнена из-за ошибки. Сообщение об ошибке: {ex}");
            }
        }
        public void UnsubcribeAll()
        {
            try
            {
                _logger?.Debug($"[{ClientName}] Отмена всех подписок");
                if (_server != null)
                    foreach (Subscription subscriptionGroup in _server.Subscriptions)
                    {
                        _server.CancelSubscription(subscriptionGroup);
                    }
                _logger?.Debug($"[{ClientName}] Отмена всех подписок выполнена успешно");
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Отмена всех подписок не выполнена из-за ошибки. Сообщение об ошибке: {ex}");
            }
        }
        public IEnumerable<OpcDaWriteResult> WriteData(IDictionary<string, object> tagsValues)
        {
            try
            {
                ItemValue[] itemValueArray = new ItemValue[tagsValues.Count()];
                int i = 0;
                foreach (var tagValue in tagsValues)
                {
                    itemValueArray[i] = new ItemValue(new Opc.ItemIdentifier(tagValue.Key))
                    {
                        Value = tagValue.Value,
                        Timestamp = DateTime.Now,
                        Quality = Quality.Good
                    };
                    i++;
                }

                var diagTimer = new Stopwatch();
                diagTimer.Start();
                IdentifiedResult[] identifiedResultsArray = _server.Write(itemValueArray);
                diagTimer.Stop();

                var opcWriteResults = new List<OpcDaWriteResult>();
                int errorsCount = 0;
                foreach (var identifiedResult in identifiedResultsArray)
                {
                    opcWriteResults.Add(new OpcDaWriteResult(identifiedResult));
                    if (identifiedResult.ResultID != ResultID.S_OK)
                        errorsCount++;
                }
                _logger?.Debug($"[{ClientName}] Записано [{itemValueArray.GetLength(0)}] значений тегов. Затраченное время = [{diagTimer.Elapsed}] Количество ошибок = [{errorsCount}]");

                return opcWriteResults;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка записи тегов. Сообщение об ошибке: {ex}");
                return null;
            }
        }
        public Task<IEnumerable<OpcDaWriteResult>> WriteDataAsync(IDictionary<string, object> tagsValues, CancellationToken token = default)
        {
            return Task.Run(() => WriteData(tagsValues), token);
        }
        public OpcDaWriteResult WriteData(string tagName, object tagValue)
        {
            return WriteData(new Dictionary<string, object>() { { tagName, tagValue } }).FirstOrDefault();
        }
        public Task<OpcDaWriteResult> WriteDataAsync(string tagName, object tagValue, CancellationToken token = default)
        {
            return Task.Run(() => WriteData(tagName, tagValue), token);
        }
        public IdentifiedResult[] WriteDataByGroup(IDictionary<string, object> tagsValues)
        {
            try
            {
                var subState = new SubscriptionState
                {
                    Active = true,
                    Deadband = 10000,
                    UpdateRate = 50,
                    Name = "WriteDataGroup (New)"
                };
                var writeGroup = (Subscription)_server.CreateSubscription(subState);

                var items = new Item[tagsValues.Count()];
                int index = 0;
                foreach (var tagValue in tagsValues)
                {
                    items[index] = new Item(new ItemIdentifier(tagValue.Key))
                    {
                        ItemName = tagValue.Key
                    };
                    index++;
                }
                writeGroup.AddItems(items);


                ItemValue[] itemValueArray = new ItemValue[tagsValues.Count()];
                int i = 0;
                foreach (var tagValue in tagsValues)
                {
                    itemValueArray[i] = new ItemValue(new Opc.ItemIdentifier(tagValue.Key))
                    {
                        Value = tagValue.Value,
                        Timestamp = DateTime.Now,
                        Quality = Quality.Good,
                        ServerHandle = writeGroup.Items[i].ServerHandle
                    };
                    i++;
                }

                var diagTimer = new Stopwatch();
                diagTimer.Start();
                IdentifiedResult[] identifiedResultsArray = writeGroup.Write(itemValueArray);
                diagTimer.Stop();

                int errorsCount = 0;
                foreach (var identifiedResult in identifiedResultsArray)
                {
                    if (identifiedResult.ResultID != ResultID.S_OK)
                        errorsCount++;
                }
                _logger?.Debug($"[{ClientName}] Записано [{itemValueArray.GetLength(0)}] значений тегов. Затраченное время = [{diagTimer.Elapsed}] Количество ошибок = [{errorsCount}]");

                return identifiedResultsArray;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка записи тегов. Сообщение об ошибке: {ex}");
                return null;
            }
        }
        public IEnumerable<OpcDaReadResult> ReadData(IEnumerable<string> tags)
        {
            try
            {
                var items = new List<Item>();
                foreach (var tag in tags)
                    items.Add(new Item(new ItemIdentifier(tag)));

                var diagTimer = new Stopwatch();
                diagTimer.Start();
                var itemsValueResults = _server.Read(items.ToArray());
                diagTimer.Stop();

                int errorsCount = 0;
                var opcDaReadResults = new List<OpcDaReadResult>();
                foreach (var itemValueResult in itemsValueResults)
                {
                    opcDaReadResults.Add(new OpcDaReadResult(itemValueResult));
                    if (itemValueResult.ResultID != ResultID.S_OK)
                        errorsCount++;
                }

                _logger?.Debug($"[{ClientName}] Прочитано [{opcDaReadResults.Count()}] значений тегов. Затраченное время = [{diagTimer.Elapsed}] Количество ошибок = [{errorsCount}]");

                return opcDaReadResults;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[{ClientName}] Ошибка чтения тегов. Сообщение об ошибке: {ex}");
                return null;
            }

        }
        public Task<IEnumerable<OpcDaReadResult>> ReadDataAsync(IEnumerable<string> tags, CancellationToken token = default)
        {
            return Task.Run(() => ReadData(tags), token);
        }
        public OpcDaReadResult ReadData(string tagName)
        {
            return ReadData(new List<string>() { tagName }).FirstOrDefault();
        }
        public Task<OpcDaReadResult> ReadDataAsync(string tagName, CancellationToken token = default)
        {
            return Task.Run(() => ReadData(tagName), token);
        }
        public IEnumerable<OpcDaReadResult> ReadBigData(IEnumerable<string> tagNames)
        {
            var groups = tagNames.Split(_maxOperationItemsCount);
            var results = new List<OpcDaReadResult>();
            foreach (var group in groups)
                results.AddRange(ReadData(group));

            return results.ToArray();
        }
        public IEnumerable<OpcDaWriteResult> WriteBigData(IDictionary<string, object> tagsValues)
        {
            var groups = tagsValues.Split(_maxOperationItemsCount);
            var results = new List<OpcDaWriteResult>();
            foreach (var group in groups)
                results.AddRange(WriteData(group));

            return results;
        }
        public void Dispose()
        {
            Disconnect();
            UnregisterLogger();
        }
        public int GetSubscriptionsCount()
        {
            if (_server != null)
            {
                return _server.Subscriptions.Count;
            }
            else
            {
                return 0;
            }
        }

        public BrowseElement[] BrowseElements(ItemIdentifier itemIdentifier, BrowseFilters browseFilters, out BrowsePosition browsePosition)
        {
            return _server.Browse(itemIdentifier, browseFilters, out browsePosition);
        }

        public ItemPropertyCollection[] GetProperties(IEnumerable<string> itemNames, PropertyID[] propertyIDs, bool returnValues)
        {
            var items = new List<ItemIdentifier>();
            itemNames.ToList().ForEach(a => items.Add(new ItemIdentifier(a)));
            return _server.GetProperties(items.ToArray(), propertyIDs, returnValues);
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
