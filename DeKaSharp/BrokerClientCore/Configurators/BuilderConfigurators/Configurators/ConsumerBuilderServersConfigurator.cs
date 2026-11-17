using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Enums;
using System.Collections.Frozen;

namespace DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Configurators
{
    internal class ConsumerBuilderServersConfigurator : ConsumerBuilderBaseConfigurator, IBuilderConfiguratorInstanceDescriptor<ConsumerBuilderServersConfigurator>
    {
        public static string Name => "BUILDER_SERVERS";

        public static ConsumerBuilderServersConfigurator Create(List<InnerBuilderParameter> parameters) => new ConsumerBuilderServersConfigurator(parameters);

        protected override Dictionary<string, ParameterRequirement> ParameterRequierments { get; } = new()
        {
            ["BOOTSTRAP_SERVERS"] = new(ParameterRequiredFormat.String, ParameterValueRequired.True, ParameterAllowMultipleValues.True)
        };

        public ConsumerBuilderServersConfigurator(List<InnerBuilderParameter> parameters) : base(parameters) { }

        protected override void Configure<TKey, TValue>(ConsumerBuilder<TKey, TValue> consumerBuilder, FrozenSet<InnerBuilderParameter> parameters)
        {
            var bootstrapServers = parameters
                .Where(par => par.Key == "BOOTSTRAP_SERVERS")
                .Select(par => par.Value)
                .ToArray();

            consumerBuilder.WithBootstrapServers(bootstrapServers);
        }
    }
}
