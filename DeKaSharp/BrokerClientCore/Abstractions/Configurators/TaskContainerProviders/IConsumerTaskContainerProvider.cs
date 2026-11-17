using Dekaf;
using DeKaSharp.BrokerClientCore.Tasks;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface IConsumerTaskContainerProvider : ITaskContainerProvider
    {
        public (bool success, ConsumerTaskContainerBase<TKey, TValue>? container, List<Enum>? errorParametersList) CreateContainer<TKey, TValue>(Guid id, IParametersProvider parametersProvider, ConsumerBuilder<TKey, TValue> clientBuilder);
    }
}
