using Dekaf;
using Dekaf.Consumer;

namespace DeKaSharp.BrokerClientCore.Builders
{
    internal class ConsumerClient<TKey, TValue> : IKafkaClient<IKafkaConsumer<TKey, TValue>>
    {
        public IKafkaConsumer<TKey, TValue> Client { get; }

        public Dictionary<ClientParameter, string>? Params { get; }

        public ConsumerClient(IKafkaConsumer<TKey, TValue> consumer, Dictionary<ClientParameter, string>? parameters = null)
        {
            Client = consumer;

            Params = parameters;
        }
    }

    internal class ConsumerInstanceBuilder<TKey, TValue>
        : ClientInstanceBuilderBase<ConsumerBuilder<TKey, TValue>, IKafkaClient<IKafkaConsumer<TKey, TValue>>, TKey, TValue>
    {
        public ConsumerInstanceBuilder() : base(Kafka.CreateConsumer<TKey, TValue>) {}

        protected override IKafkaClient<IKafkaConsumer<TKey, TValue>> BuildClient()
        {
            var innerClient = ClientBuilder!
                .BuildAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (Params!.Count == 0)
            {
                Logger.Log($"Создан консъюмер с id: {InstanceId} и null параметрами");

                return new ConsumerClient<TKey, TValue>(innerClient, null);
            }
            else
            {
                Logger.Log($"Создан консъюмер с id: {InstanceId} и имеющимися параметрами");

                return new ConsumerClient<TKey, TValue>(innerClient, Params);
            }
        }
    }
}
