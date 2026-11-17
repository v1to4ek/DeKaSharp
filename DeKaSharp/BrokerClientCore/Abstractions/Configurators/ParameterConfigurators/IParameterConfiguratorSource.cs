namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators
{
    internal interface IParameterConfiguratorSource
    {
        public Enum ParameterEnum { get; }

        IParameterConfigurator GetConfigurator(string parameterValue);
    }
}
