using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Configurators
{
    internal sealed class ExceptionOnErrorConfigurator : UniversalParameterConfiguratorBase<bool>, IParameterConfiguratorDescriptor<ExceptionOnErrorConfigurator>
    {
        public static string Name => "THROW_EX_ON_ERROR";

        public static ExceptionOnErrorConfigurator Create(IDependencyProvider registryProvider) => new ExceptionOnErrorConfigurator(registryProvider);

        public override Enum ParameterEnum => BaseClientParameter.ThrowExOnError;

        private ExceptionOnErrorConfigurator(IDependencyProvider registryProvider) : base(registryProvider) { }

    }
}
