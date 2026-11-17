using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators
{
    internal interface IBuilderConfiguratorClientDescriptor
    {
        public static abstract ClientType ClientType { get; }
    }
}
