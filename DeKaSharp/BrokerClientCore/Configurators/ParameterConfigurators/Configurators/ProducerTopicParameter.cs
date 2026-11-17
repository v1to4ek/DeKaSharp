using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Configurators
{
    internal sealed class ProducerTopicParameter : ClientParameterConfiguratorBase<string>, IParameterConfiguratorDescriptor<ProducerTopicParameter>
    {
        public static string Name => "PRODUCER_TOPIC";

        public static ProducerTopicParameter Create(IDependencyProvider registryProvider) => new ProducerTopicParameter(registryProvider);

        public override Enum ParameterEnum => ProducerTaskContainerBaseParameters.Topic;

        protected override ClientType ClientType => ClientType.Producer;

        private ProducerTopicParameter(IDependencyProvider registryProvider) : base(registryProvider) { }
    }
}
