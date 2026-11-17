using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators
{
    internal interface IBuilderConfigurator
    {
        public ClientType ClientType { get; }
    }
}
