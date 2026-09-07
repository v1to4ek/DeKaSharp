using Dekaf;
using Dekaf.Consumer;
using Dekaf.Producer;
using DeKaSharp.BrokerTaskBuilder.Containers;

namespace DeKaSharp
{
    internal class BrokerService
    {
        private readonly List<Task> _brokerTasks;

        private readonly BrokerTaskBuilder.BrokerTaskBuilder _taskBuilder;

        private readonly InputRouter _inputRouter;

        private readonly TokenGenerator _tokenGenerator;

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

            _brokerTasks = [];

            _taskBuilder = new();

            _inputRouter = new();

            _tokenGenerator = new();
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

            var ct = _tokenGenerator.GetOrCreateAndGet();

            _taskBuilder.AddTaskContainer(
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

            var ct = _tokenGenerator.GetOrCreateAndGet();

            _taskBuilder.AddTaskContainer(
                () => new ConsumerTaskContainer(consumerId, consumer, callback, ct));

            _state = ServiceState.Registered;

            return consumerId;
        }

        public async Task StartServiceAsync()
        {

        }

        private void RunBrokerTask()
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
