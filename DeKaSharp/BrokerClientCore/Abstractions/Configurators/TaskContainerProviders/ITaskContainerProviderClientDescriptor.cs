using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface ITaskContainerProviderClientDescriptor
    {
        public static abstract ClientType ClientType { get; }
    }
}
