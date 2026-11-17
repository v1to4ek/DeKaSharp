using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;

namespace DeKaSharp.BrokerClientCore.Abstractions
{
    internal interface IBuilderConfiguratorInstanceDescriptor<TSelf> where TSelf : IBuilderConfiguratorSource<IBuilderConfigurator>
    {
        public static abstract string Name { get; }

        public static abstract TSelf Create(List<InnerBuilderParameter> parameters);
    }
}
