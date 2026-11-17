using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface ITaskContainerProvider
    {
        public ClientType ClientType { get; }
    }
}
