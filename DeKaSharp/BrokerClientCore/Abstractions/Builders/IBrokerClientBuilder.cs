using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Abstractions.TaskContainers;
using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Builders
{
    internal interface IBrokerClientBuilder
    {
        public ClientType ClientType { get; }

        public BuilderBehaviorType BehaviorType { get; }

        public Guid Id { get; }

        public void ConfigureWith(IBuilderConfigurator configurator);

        public void ConfigureWith(IParameterConfigurator configurator);

        public void ConfigureWith(ITaskContainerConfigurator configurator);

        public void ConfigureWith(ITaskContainerProvider provider);

        public IBrokerTaskContainer Build();
    }
}
