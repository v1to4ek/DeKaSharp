using DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Enums;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators
{
    internal interface IParameterConfigurator
    {
        public ConfiguratorClientType ClientType { get; }

        public abstract Enum ParameterEnum { get; }

        public void Configure(IParametersProvider parametersProvider);
    }
}
