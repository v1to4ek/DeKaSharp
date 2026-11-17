namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators
{
    internal interface IBuilderConfiguratorSource<out TBuilderConfigurator> where TBuilderConfigurator : IBuilderConfigurator
    {
        public TBuilderConfigurator GetBuilderConfigurator();
    }
}
