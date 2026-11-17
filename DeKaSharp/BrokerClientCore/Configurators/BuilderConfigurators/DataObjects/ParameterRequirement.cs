using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Enums;

namespace DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects
{
    internal readonly struct ParameterRequirement
    {
        public ParameterRequiredFormat FormatTypeRequirement { get; init; }

        public ParameterValueRequired NoValueRequirement { get; init; }

        public ParameterAllowMultipleValues MultipleValuesRequirement { get; init; }

        public ParameterRequirement(ParameterRequiredFormat formatTypeRequirement,
            ParameterValueRequired noValueRequirement,
            ParameterAllowMultipleValues multipleValuesRequirement)
        {
            FormatTypeRequirement = formatTypeRequirement;

            NoValueRequirement = noValueRequirement;

            MultipleValuesRequirement = multipleValuesRequirement;
        }
    }
}
