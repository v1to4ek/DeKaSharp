using Dekaf;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators
{
    internal interface IConsumerBuilderConfigurator : IBuilderConfigurator
    {
        public void Configure<TKey, TValue>(ConsumerBuilder<TKey, TValue> consumerBuilder);
    }
}
