using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators
{
    internal interface IParameterConfiguratorDescriptor<TSource> where TSource : IParameterConfiguratorSource
    {
        public static abstract string Name { get; }

        public static abstract TSource Create(IDependencyProvider registryProvider);
    }
}
