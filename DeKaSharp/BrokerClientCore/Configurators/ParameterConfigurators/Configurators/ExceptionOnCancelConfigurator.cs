using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Configurators
{
    internal sealed class ExceptionOnCancelConfigurator : UniversalParameterConfiguratorBase<bool>, IParameterConfiguratorDescriptor<ExceptionOnCancelConfigurator>
    {
        public static string Name => "THROW_EX_ON_CANCEL";

        public static ExceptionOnCancelConfigurator Create(IDependencyProvider registryProvider) => new ExceptionOnCancelConfigurator(registryProvider);

        public override Enum ParameterEnum => BaseClientParameter.ThrowExOnCancellation;

        private ExceptionOnCancelConfigurator(IDependencyProvider registryProvider) : base(registryProvider) { }
    }
}
