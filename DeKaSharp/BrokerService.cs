using Dekaf;
using DeKaSharp.BrokerTaskBuilder;
using DeKaSharp.BrokerTaskBuilder.Containers;

namespace DeKaSharp
{
    internal class BrokerService
    {
        private readonly List<Task> _brokerTasks;

        private readonly BrokerTaskHandler _taskHandler;

        private readonly InputRouter _inputRouter;

        private readonly TokenGenerator _cancellationGenerator;

        private ServiceState _state;

        private enum ServiceState
        {
            Empty,
            Registered,
            Running
        }

        public BrokerService()
        {
            _state = ServiceState.Empty;

            _cancellationGenerator = new();

            _brokerTasks = [];

            _taskHandler = new();

            _inputRouter = new();

            Logger.Log("Создан сервис брокера");
        }

        public string RegisterProducer(string server, string topic)
        {
            if(_state == ServiceState.Running)
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

            _taskHandler.AddTaskContainer(
                () => new ProducerTaskContainer(producerId, topic, producer, _inputRouter, ct));

            _state = ServiceState.Registered;

            return producerId;
        }

        public string RegisterConsumer(string server, string group, string topic, Action<string,string> callback)
        {
            if (_state == ServiceState.Running)
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

            _taskHandler.AddTaskContainer(
                () => new ConsumerTaskContainer(consumerId, consumer, callback, ct));

            _state = ServiceState.Registered;

            return consumerId;
        }

        public async Task StartServiceAsync()
        {
            Logger.Log("Запуск сервиса брокера");

            if(_state == ServiceState.Running) throw new InvalidOperationException("Сервис уже запущен");

            if(_state == ServiceState.Empty) throw new InvalidOperationException("Сервис не имеет зарегистрированных продьюсеров или консъюмеров");

            _state = ServiceState.Running;

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
            catch (Exception)
            {

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

        private void RunBrokerTasks()
        {

        }


        public void StopService()
        {

        }

        public void ProduceMessage()
        {

        }
    }
}
