using Dekaf;
using Dekaf.Consumer;
using Dekaf.Producer;
using DeKaSharp.BrokerClientCore.Builders;

namespace DeKaSharp.BrokerClientCore
{
    internal static class BrokerClientBuilderFactory
    {
        public static (ProducerInstanceBuilder<TKey, TValue>, string builderId) CreateProducerBuilder<TKey, TValue>()
        {
            var builder = new ProducerInstanceBuilder<TKey, TValue>();

            var id = builder.InstanceId.ToString();

            return (builder, id);
        }

        public static (ConsumerInstanceBuilder<TKey, TValue>, string builderId) CreateConsumerBuilder<TKey, TValue>()
        {
            var builder = new ConsumerInstanceBuilder<TKey, TValue>();

            var id = builder.InstanceId.ToString();

            return (builder, id);
        }
    }
}
