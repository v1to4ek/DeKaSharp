using Dekaf;
using Dekaf.Consumer;
using Dekaf.Producer;
using DeKaSharp.BrokerTaskBuilder;
using DeKaSharp.BrokerTaskBuilder.Containers;

namespace DeKaSharp
{
    internal class BrokerService : IAsyncCleanable
    {
        private readonly List<Task> _brokerTasks;

        private readonly BrokerTaskHandler _taskHandler;

        private readonly ChannelRouter _inputRouter;

        private readonly ChannelRouter _outputRouter;

        private readonly CancellationTokenGenerator _cancellationGenerator;

        private readonly CleanerService _cleanerService;

        private readonly Lock _serviceLocker;

        private ServiceState _serviceState;

        public event Action<string>? OnServiceStoppedCallback;

        public event Action<string>? OnServiceErrorCallback;

        public int CleanerPriority => -1;

        private enum ServiceState
        {
            Empty,
            Registered,
            Running
        }

        public BrokerService()
        {
            _serviceState = ServiceState.Empty;

            _brokerTasks = [];

            _serviceLocker = new();

            _cancellationGenerator = new();

            _taskHandler = new();

            _inputRouter = new();

            _outputRouter = new();

            _cleanerService = new();

            RegisterServicesForCleaning();

            Logger.Log("Создан сервис брокера");
        }

        private void RegisterServicesForCleaning()
        {
            _cleanerService.RegisterItem(_cancellationGenerator);
            _cleanerService.RegisterItem(_inputRouter);
            _cleanerService.RegisterItem(_outputRouter);
            _cleanerService.RegisterItem(_taskHandler);
            _cleanerService.RegisterItem(this);
        }

        /// <summary>
        /// Регистрация продьюсера без коллбэка
        /// </summary>
        public string RegisterProducer(string server, string topic)
        {
            var producerTuple = RegisterProducerCore(server);

            var producerId = producerTuple.producerId;

            var producer = producerTuple.producer;

            var ct = producerTuple.ct;

            _taskHandler.AddTaskContainer(
                () => new ProducerTaskContainer(producerId, topic, producer, _inputRouter, ct));

            _serviceState = ServiceState.Registered;

            return producerId;
        }

        /// <summary>
        /// Регистрация продьюсера с коллбэком на результат отправки
        /// </summary>
        public string RegisterProducer(string server, string topic, Action<string> onSentCallback)
        {
            var producerTuple = RegisterProducerCore(server);

            var producerId = producerTuple.producerId;

            var producer = producerTuple.producer;

            var ct = producerTuple.ct;

            _taskHandler.AddTaskContainer(
                () => new ProducerTaskContainer(producerId, topic, producer, _inputRouter, onSentCallback, ct));

            _serviceState = ServiceState.Registered;

            return producerId;
        }

        private (string producerId, IKafkaProducer<string,string> producer, CancellationToken ct) RegisterProducerCore(string server)
        {
            if (_serviceState == ServiceState.Running)
            {
                throw new InvalidOperationException("Невозможно добавить продьюсер в запущенный сервис");
            }

            var producerId = Guid.NewGuid().ToString();

            var producer = Kafka.CreateProducer<string, string>()
                .WithBootstrapServers(server)
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            _inputRouter.RegisterChannel(producerId);

            var ct = _cancellationGenerator.GetOrCreateAndGet();

            return (producerId, producer, ct);
        }

        /// <summary>
        /// Регистрация консъюмера с каналом для записи
        /// </summary>
        public string RegisterConsumer(string server, string group, string topic)
        {
            var consumerTuple = RegisterConsumerCore(server, group, topic);

            var consumerId = consumerTuple.consumerId;

            var consumer = consumerTuple.consumer;

            var ct = consumerTuple.ct;

            _taskHandler.AddTaskContainer(
                () => new ConsumerTaskContainer(consumerId, consumer, _outputRouter, ct));

            _serviceState = ServiceState.Registered;

            return consumerId;
        }

        /// <summary>
        /// Регистрация консъюмера с коллбэком для получения сообщения
        /// </summary>
        public string RegisterConsumer(string server, string group, string topic, Action<string,string> outputCallback)
        {
            var consumerTuple = RegisterConsumerCore(server, group, topic);

            var consumerId = consumerTuple.consumerId;

            var consumer = consumerTuple.consumer;

            var ct = consumerTuple.ct;

            _taskHandler.AddTaskContainer(
                () => new ConsumerTaskContainer(consumerId, consumer, outputCallback, ct));

            _serviceState = ServiceState.Registered;

            return consumerId;
        }

        private (string consumerId, IKafkaConsumer<string,string> consumer, CancellationToken ct) RegisterConsumerCore(string server, string group, string topic)
        {
            if (_serviceState == ServiceState.Running)
            {
                throw new InvalidOperationException("Невозможно добавить консъюмер в запущенный сервис");
            }

            var consumerId = Guid.NewGuid().ToString();

            var consumer = Kafka.CreateConsumer<string, string>()
                .WithBootstrapServers(server)
                .WithGroupId(group)
                .SubscribeTo(topic)
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            var ct = _cancellationGenerator.GetOrCreateAndGet();

            return (consumerId, consumer, ct);
        }

        public bool SubscribeToBrokerError(string id, Action<string> callback)
        {
            try
            {
                if ( _serviceState != ServiceState.Registered)
                {
                    throw new InvalidOperationException("Невозможно подписаться на событие ошибки: сервис пуст либо уже запущен");
                }

                _taskHandler.SubscribeToErrorById(id, callback);

                Logger.Log($"Успешно выполнена подписка на событие ошибки в контейнере с id: {id}");

                return true;
            }
            catch(Exception ex)
            {
                Logger.Log($"Ошибка при подписке на событие ошибки в контейнере с id: {id}. Текст ошибки: {ex.Message} ");

                return false;
            }
        }

        public async Task StartServiceAsync()
        {
            Logger.Log("Запуск сервиса брокера");

            if(_serviceState == ServiceState.Running) throw new InvalidOperationException("Сервис уже запущен");

            if(_serviceState == ServiceState.Empty) throw new InvalidOperationException("Сервис не имеет зарегистрированных продьюсеров или консъюмеров");

            _serviceState = ServiceState.Running;

            var ct = _cancellationGenerator.GetOrCreateAndGet();

            var taskFactories = LoadTaskFactories();

            Logger.Log("Запуск задач брокера...");

            taskFactories.ForEach(factory =>
            {
                var brokerTypeName = factory.type == BrokerTaskType.Consumer ? "консъюмер" : "продьюсер";

                Logger.Log($"Запуск асинхронной задачи брокера: {brokerTypeName}, id: {factory.id}");

                _brokerTasks.Add(factory.BuildTask());

                Logger.Log($"Асинхронная задача брокера запущена: {brokerTypeName}, id: {factory.id}");
            });

            Logger.Log("Все задачи брокера запущены");

            var routerServiceTask = _inputRouter.RunAsync(ct);

            var brokerServiceTask = Task.WhenAll(_brokerTasks);

            var generalTask = Task.WhenAll(brokerServiceTask, routerServiceTask);

            try
            {
                await generalTask;
            }
            catch(OperationCanceledException) when (generalTask.IsCanceled)
            {
                Logger.Log("Выполнение сервиса было остановлено");
            }
            catch(Exception ex)
            {
                OnServiceErrorCallback?.Invoke($"Произошла неожиданная ошибка при выполнении сервиса: {ex.Message}");

                Logger.Log($"Произошла неожиданная ошибка при выполнении сервиса: {ex.Message}.");
            }
            finally
            {
                await ClearAllAsync();

                OnServiceStoppedCallback?.Invoke($"Выполнена остановка сервиса.");

                Logger.Log("Сервис полностью остановлен");
            }
        }

        private List<(string id, BrokerTaskType type, Func<Task> BuildTask)> LoadTaskFactories()
        {
            var brokerTaskFactoryWrappers = _taskHandler.BuildTasks();

            var taskFactories = new List<(string id, BrokerTaskType type, Func<Task> BuildTask)>();

            var consumerTaskCount = brokerTaskFactoryWrappers
                .Count(item => item.Type == BrokerTaskType.Consumer);

            var producerTaskCount = brokerTaskFactoryWrappers
                .Count(item => item.Type == BrokerTaskType.Producer);

            Logger.Log($"Регистрация фабрик задач брокера: {consumerTaskCount} консъюмеров, {producerTaskCount} продьюсеров");

            var count = 0;

            foreach (var item in brokerTaskFactoryWrappers)
            {
                Logger.Log($"Передача фабрики задачи брокера: {++count} из {brokerTaskFactoryWrappers.Count()}");

                if (!item.IsSuccess)
                {
                    Logger.Log($"Получена ошибка при создании фабрики задачи брокера {count}: {item.ErrorMessage}");

                    continue;
                }
                else
                {
                    var brokerTypeName = item.Type == BrokerTaskType.Consumer ? "консъюмер" : "продьюсер";

                    taskFactories.Add((item.Id!, item.Type, item.TaskFactory!));

                    Logger.Log($"Успешно добавлена фабрика задачи брокера {count}: тип: {brokerTypeName}, id: {item.Id}");
                }

                Logger.Log($"Регистрация фабрик задач завершена");
            }

            return taskFactories;
        }

        public bool StopService()
        {
            if(_serviceState != ServiceState.Running)
            {
                Logger.Log("Попытка остановки сервиса, который не запущен");

                return false;
            }
            else
            {
                _cancellationGenerator.Cancel();

                Logger.Log("Запрошена остановка сервиса брокера");

                return true;
            }
        }

        /// <summary>
        /// Метод для отправки одного сообщения
        /// </summary>
        public bool TryProduceMessage(InputMessage message)
        {
            if(_serviceState != ServiceState.Running)
            {
                Logger.Log("Попытка отправки сообщения в сервис, который не запущен");

                return false;
            }
            try
            {
                var result = _inputRouter.PublishItem(message);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка при записи сообщения с id :{message.Id} в канал: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Метод для синхронного получения одного сообщения
        /// </summary>
        public (bool success, OutputMessage? message) TryConsumeMessageSynchronously(string consumerId)
        {
            var (found, channel) = _outputRouter.GetChannelById(consumerId);

            if (!found) return (false, null);

            var outChannel = channel!;

            if (outChannel.Reader.TryRead(out var message))
            {
                Logger.Log($"Успешно прочитано сообщение из канала с id: {consumerId}");

                return (true, message);
            }

            Logger.Log($"Ошибка чтения из канала с id: {consumerId}");

            return (false, null);
        }

        private Task ClearAllAsync() => _cleanerService.CleanParallelAsync();

        public Task CleanAsync()
        {
            _brokerTasks.Clear();

            return Task.CompletedTask;
        }
    }
}
