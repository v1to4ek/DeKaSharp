using Dekaf;
using DeKaSharp.BrokerClientCore.Tasks;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface IProducerTaskContainerProvider : ITaskContainerProvider
    {
        public (bool success, ProducerTaskContainerBase<TKey, TValue>? container, List<Enum>? errorParametersList) CreateContainer<TKey, TValue>(Guid id, IParametersProvider parametersProvider, ProducerBuilder<TKey, TValue> clientBuilder);
    }
}
