using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Parameters;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IParametersProvider : IDataProvider
    {
        public void SetParameter<TValue>(Enum parameterName, TValue parameterValue);

        public (bool success, IParametersSource? provider, List<Enum>? missingParameters) TryCreateParameters(ParametersValidator validator);
    }
}
