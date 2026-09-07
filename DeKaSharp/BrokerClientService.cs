using Dekaf;
using Dekaf.Consumer;
using Dekaf.Producer;
using System.Collections.Concurrent;

namespace DeKaSharp
{
    internal class BrokerClientService
    {
        private readonly ConcurrentDictionary<string, ProducerWrapper<string, string>> _producerDictionary;

        private readonly ConcurrentDictionary<string, IKafkaConsumer<string, string>> _consumerDictionary;

        private readonly List<Task> _consumerTasks;

        private readonly List<Task> _producerTasks;

        private readonly Lock _stateLocker;

        private InputRouter _router;

        private CancellationTokenSource _cts;

        private ServiceState _serviceState;

        private bool _stopped;

        public delegate void StoppingCallbackDelegate();

        private record class ProducerWrapper<TKey, TValue>
        {
            public required IKafkaProducer<TKey, TValue> Producer { get; set; }

            public required string TopicName { get; set; }
        }

        private enum ServiceState
        {
            Empty,
            OnlyProducersRegistered,
            OnlyConsumersRegistered,
            AllRegistered,
            Started
        }

        public BrokerClientService()
        {
            _stateLocker = new Lock();

            _producerDictionary = [];

            _consumerDictionary = [];

            _consumerTasks = [];

            _producerTasks = [];

            _stopped = false;

            _cts = new CancellationTokenSource();

            _router = new InputRouter();

            Logger.Log("Создан сервис брокера");
        }

        public bool InputData(InputMessage message)
        {
            if(_serviceState == ServiceState.Empty)
            {
                Logger.Log($"Некорректное состояние свервиса {ServiceState.Empty}");
                return false;
            }

            if(_consumerDictionary.ContainsKey(message.Id) || _producerDictionary.ContainsKey(message.Id))
            {
                _router.PublishItem(message);
                return true;
            }
            else
            {
                Logger.Log($"Канал с id: {message.Id} не найден");
                return false;
            }
        }

        public string AddProducer(string serverId, string topicName)
        {
            if (_serviceState != ServiceState.Empty) throw new InvalidOperationException("Невозможно добавить продьюсер после запуска сервиса");

            var producerId = Guid.NewGuid().ToString();

            var producer = new ProducerWrapper<string, string>
            {
                Producer = Kafka.CreateProducer<string, string>()
                .WithBootstrapServers(serverId)
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult(),
                TopicName = topicName
            };

            var added = _producerDictionary.TryAdd(producerId, producer);

            if (added) Logger.Log($"Добавлен продьюссер с id: {producerId}");
            else throw new Exception("Ошибка добавления продьюссера в коллекцию");

            _router.RegisterChannel(producerId);

            return producerId;
        }

        public string AddConsumer(string server, string group, string topic)
        {
            if (_serviceState != ServiceState.Empty) throw new InvalidOperationException("Невозможно добавить консъюмер после запуска сервиса");

            var consumerId = Guid.NewGuid().ToString();

            _consumerDictionary.TryAdd(consumerId, Kafka.CreateConsumer<string, string>()
                .WithBootstrapServers(server)
                .WithGroupId(group)
                .SubscribeTo(topic)
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult());

            return consumerId;
        }

        public async Task StartServiceAsync(StoppingCallbackDelegate stoppingCallback)
        {
            Logger.Log("Запуск сервиса брокера");

            lock (_stateLocker)
            {
                if (_serviceState != ServiceState.Empty) throw new Exception("Сервис уже запущен");

                if (_stopped)
                {
                    Logger.Log("Ранее сервис был остановлен: пересоздание зависимостей");

                    _cts = new CancellationTokenSource();

                    _router = new InputRouter();

                    _stopped = false;
                }
            }

            var stoppingToken = _cts.Token;

            Logger.Log($"Запуск {_producerDictionary.Count} обработчиков-продьюсеров");
            RegisterProducersTasks(stoppingToken);

            Logger.Log($"Запуск {_consumerDictionary.Count} обработчиков-консъюмеров");
            RegisterConsumersTasks(stoppingToken);

            var exceptionalState = false;

            Logger.Log($"Запуск сервиса роутера");
            var routerTask = _router.Start(stoppingToken);

            Task generalTask;

            if(_serviceState == ServiceState.AllRegistered)
            {
                var consumersTasks = Task.WhenAll(_consumerTasks);

                var producersTasks = Task.WhenAll(_producerTasks);

                generalTask = Task.WhenAll(consumersTasks, producersTasks, routerTask);
            }
            else if(_serviceState == ServiceState.OnlyProducersRegistered)
            {
                var producersTasks = Task.WhenAll(_producerTasks);

                generalTask = Task.WhenAll(producersTasks, routerTask);
            }
            else if(_serviceState == ServiceState.OnlyConsumersRegistered)
            {
                var consumersTasks = Task.WhenAll(_consumerTasks);

                generalTask = Task.WhenAll(consumersTasks, routerTask);
            }
            else
            {
                Logger.Log($"Обнаружено исключительное состояние: {_serviceState}");

                exceptionalState = true;

                generalTask = Task.CompletedTask;
            }

            try
            {
                if (exceptionalState) throw new Exception("Недопустимое состояние сервиса брокера");

                _serviceState = ServiceState.Started;

                await generalTask;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }
            finally
            {
                Logger.Log("Остановка сервиса, финализация зависимостей");

                var consumerClearingTask = Task.Run(async () =>
                {
                    foreach (var consumer in _consumerDictionary.Values)
                    {
                        await consumer.CloseAsync();

                        await consumer.DisposeAsync();
                    }

                    _consumerDictionary.Clear();

                    Logger.Log("Словарь консъюмеров очищен");
                },
                CancellationToken.None);

                var producerClearingTask = Task.Run(async () =>
                {
                    foreach (var producer in _producerDictionary.Values)
                    {
                        await producer.Producer.FlushAsync();

                        await producer.Producer.DisposeAsync();
                    }

                    _producerDictionary.Clear();

                    Logger.Log("Словарь продьюсеров очищен");
                },
                CancellationToken.None);

                await Task.WhenAll(producerClearingTask, consumerClearingTask);

                _router.Dispose();
                Logger.Log("Каналы роутера очищены");

                _cts.Dispose();
                Logger.Log("Токен отмены очищен");

                _serviceState = ServiceState.Empty;

                _stopped = true;

                Logger.Log("Сервис остановлен");

                Logger.Log("Вызов коллбэка сброса блокировки");

                stoppingCallback.Invoke();
            }
        }

        public void StopService()
        {
            if (_stopped)
            {
                Logger.Log("Сервис уже остановлен - пропуск операции остановки - выброс исключения");

                throw new InvalidOperationException("Невалидная операция остановки сервиса, когда он остановлен");
            }
            else
            {
                _cts.Cancel();

                Logger.Log("Остановка сервиса (cts.Cancel)");
            }
        }

        private void RegisterProducersTasks(CancellationToken ct)
        {
            if (_producerDictionary.IsEmpty) return;

            lock (_stateLocker)
            {
                if(_serviceState == ServiceState.Empty)
                {
                    _serviceState = ServiceState.OnlyProducersRegistered;
                }
                if(_serviceState == ServiceState.OnlyConsumersRegistered)
                {
                    _serviceState = ServiceState.AllRegistered;
                }
                if(_serviceState == ServiceState.Started)
                {
                    throw new InvalidOperationException("Невозможна регистрация новых обработчиков при запущенном потоке сервиса");
                }
            }

            foreach(var kvPair in _producerDictionary)
            {
                var idKey = kvPair.Key;

                var producer = kvPair.Value;

                var channel = _router.GetChannelById(idKey);

                var producerTask = Task.Run(
                    async () =>
                    {
                        try
                        {
                            var topicName = producer.TopicName;

                            var kafkaProducer = producer.Producer;

                            await foreach (var item in channel.Reader.ReadAllAsync(ct))
                            {
                                var messageKey = item.MessageKey;

                                var messageValue = item.MessageValue;

                                await kafkaProducer.ProduceAsync(topicName, messageKey, messageValue);
                            }
                        }
                        catch(Exception ex)
                        {
                            Logger.Log(ex.Message);
                        }

                    }, 
                    ct);

                _producerTasks.Add(producerTask);

                Logger.Log($"Зарегистрирован асинхронный обработчик для id: {idKey}");
            }
        }

        private void RegisterConsumersTasks(CancellationToken ct)
        {
            if (_producerDictionary.IsEmpty) return;

            lock (_stateLocker)
            {
                if (_serviceState == ServiceState.Empty)
                {
                    _serviceState = ServiceState.OnlyConsumersRegistered;
                }
                if (_serviceState == ServiceState.OnlyProducersRegistered)
                {
                    _serviceState = ServiceState.AllRegistered;
                }
                if (_serviceState == ServiceState.Started)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
