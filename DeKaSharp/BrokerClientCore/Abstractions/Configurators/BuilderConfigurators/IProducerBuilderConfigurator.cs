using Dekaf;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators
{
    internal interface IProducerBuilderConfigurator : IBuilderConfigurator
    {
        public void Configure<TKey, TValue>(ProducerBuilder<TKey, TValue> producerBuilder);
    }
}
