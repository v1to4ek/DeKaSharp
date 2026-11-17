using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IBuilderConfiguratorsProvider : IDataProvider
    {
        public IBuilderConfigurator CreateConfigurator(string configuratorName, ClientType clientType, List<InnerBuilderParameter> builderParameters);
    }
}
