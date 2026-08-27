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

        private readonly CancellationTokenSource _cts;

        private readonly ProducingRouter _router;

        private readonly Lock _stateLocker;

        private ServiceProcessorsState _serviceState;

        public delegate void PublisherDelegate(InputMessage message);

        private record class ProducerWrapper<TKey, TValue>
        {
            public required IKafkaProducer<TKey, TValue> Producer { get; set; }

            public required string TopicName { get; set; }
        }

        private record struct MessageToProduce
        {
            public required string keyItem; 
            
            public string valueItem;
        }

        private enum ServiceProcessorsState
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

            _cts = new CancellationTokenSource();

            _producerDictionary = [];

            _consumerDictionary = [];

            _consumerTasks = [];

            _producerTasks = [];

            _router = new ProducingRouter();
        }

        public PublisherDelegate GetPublisher(string id) => _router.PublishItem;

        public string AddProducer(string serverId, string topicName)
        {
            var producerId = Guid.NewGuid().ToString();

            var producer = new ProducerWrapper<string, string>
            {
                Producer = Kafka.CreateProducer<string, string>()
                .WithBootstrapServers(serverId)
                .Build(),
                TopicName = topicName
            };

            _producerDictionary.TryAdd(producerId, producer);

            _router.RegisterChannel(producerId);

            return producerId;
        }

        public string AddConsumer(string server, string group, string topic)
        {
            var consumerId = Guid.NewGuid().ToString();

            _consumerDictionary.TryAdd(consumerId, Kafka.CreateConsumer<string, string>()
                .WithBootstrapServers(server)
                .WithGroupId(group)
                .SubscribeTo(topic)
                .Build());

            return consumerId;
        }

        public async Task StartServiceAsync()
        {
            RegisterProducersTasks();

            RegisterConsumersTasks();

            var exceptionalState = false;

            var routerTask = _router.Start();

            Task generalTask;

            if(_serviceState == ServiceProcessorsState.AllRegistered)
            {
                var consumersTasks = Task.WhenAll(_consumerTasks);

                var producersTasks = Task.WhenAll(_producerTasks);

                generalTask = Task.WhenAll(consumersTasks, producersTasks, routerTask);
            }
            else if(_serviceState == ServiceProcessorsState.OnlyProducersRegistered)
            {
                var producersTasks = Task.WhenAll(_producerTasks);

                generalTask = Task.WhenAll(producersTasks, routerTask);
            }
            else if(_serviceState == ServiceProcessorsState.OnlyConsumersRegistered)
            {
                var consumersTasks = Task.WhenAll(_consumerTasks);

                generalTask = Task.WhenAll(consumersTasks, routerTask);
            }
            else
            {
                exceptionalState = true;

                generalTask = Task.CompletedTask;
            }

            try
            {
                if (exceptionalState) throw new Exception();

                await generalTask;
            }
            catch (Exception)
            {

            }
            finally
            {
                foreach(var value in _consumerDictionary.Values) value.
            }
        }

        public void StopService() => _cts.Cancel();

        private void RegisterProducersTasks()
        {
            if (_producerDictionary.IsEmpty) return;

            lock (_stateLocker)
            {
                if(_serviceState == ServiceProcessorsState.Empty)
                {
                    _serviceState = ServiceProcessorsState.OnlyProducersRegistered;
                }
                if(_serviceState == ServiceProcessorsState.OnlyConsumersRegistered)
                {
                    _serviceState = ServiceProcessorsState.AllRegistered;
                }
                if(_serviceState == ServiceProcessorsState.Started)
                {
                    throw new InvalidOperationException();
                }
            }

            foreach(var kvPair in _producerDictionary)
            {
                var idKey = kvPair.Key;

                var producer = kvPair.Value;

                var producerTask = Task.Run(
                    async () =>
                    {
                        var topicName = producer.TopicName;

                        var kafkaProducer = producer.Producer;

                        await foreach(var item in _router.GetChannelById(idKey).Reader.ReadAllAsync())
                        {
                            var messageKey = item.MessageKey;

                            var messageValue = item.MessageValue;

                            await kafkaProducer.FireAsync(topicName, messageKey, messageValue);
                        }
                    }
                );

                _producerTasks.Add(producerTask);
            }
        }



        private void RegisterConsumersTasks()
        {
            if (_producerDictionary.IsEmpty) return;

            lock (_stateLocker)
            {
                if (_serviceState == ServiceProcessorsState.Empty)
                {
                    _serviceState = ServiceProcessorsState.OnlyConsumersRegistered;
                }
                if (_serviceState == ServiceProcessorsState.OnlyProducersRegistered)
                {
                    _serviceState = ServiceProcessorsState.AllRegistered;
                }
                if (_serviceState == ServiceProcessorsState.Started)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
