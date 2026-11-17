namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators
{
    internal interface IParametersSource
    {
        public TValue GetParameter<TValue>(Enum parameterName);
    }
}
