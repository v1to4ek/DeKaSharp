using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IParameterConfiguratorProvider : IDataProvider
    {
        public IParameterConfigurator GetConfigurator(string configuratorName, string parameterValue);
    }
}
