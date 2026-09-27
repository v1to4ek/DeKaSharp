using Dekaf;
using Dekaf.Consumer;
using Dekaf.Producer;

namespace DeKaSharp.BrokerClientCore.Builders
{
    internal class ProducerClient<TKey, TValue> : IKafkaClient<IKafkaProducer<TKey, TValue>>
    {
        public IKafkaProducer<TKey, TValue> Client { get; }

        public Dictionary<ClientParameter, string>? Params { get; }

        public ProducerClient(IKafkaProducer<TKey, TValue> producer, Dictionary<ClientParameter, string>? parameters)
        {
            Client = producer;

            Params = parameters;
        }
    }

    internal class ProducerInstanceBuilder<TKey, TValue>
        : ClientInstanceBuilderBase<ProducerBuilder<TKey, TValue>, IKafkaClient<IKafkaProducer<TKey, TValue>>, TKey, TValue>
    {
        public ProducerInstanceBuilder() : base(Kafka.CreateProducer<TKey, TValue>) {}

        protected override IKafkaClient<IKafkaProducer<TKey, TValue>> BuildClient()
        {
            var innerClient = ClientBuilder!
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (Params!.Count == 0)
            {
                Logger.Log($"Создан продьюсер с id: {InstanceId} и null параметрами");

                return new ProducerClient<TKey, TValue>(innerClient, null);
            }
            else
            {
                Logger.Log($"Создан продьюсер с id: {InstanceId} и имеющимися параметрами");

                return new ProducerClient<TKey, TValue>(innerClient, Params);
            }
        } 
    }
}
