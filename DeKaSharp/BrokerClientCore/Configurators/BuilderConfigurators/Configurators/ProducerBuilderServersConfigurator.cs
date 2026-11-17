using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Enums;
using System.Collections.Frozen;

namespace DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Configurators
{
    internal class ProducerBuilderServersConfigurator : ProducerBuilderBaseConfigurator, IBuilderConfiguratorInstanceDescriptor<ProducerBuilderServersConfigurator>
    {
        public static string Name => "BUILDER_SERVERS";

        public static ProducerBuilderServersConfigurator Create(List<InnerBuilderParameter> parameters) => new ProducerBuilderServersConfigurator(parameters);

        protected override Dictionary<string, ParameterRequirement> ParameterRequierments { get; } = new()
        {
            ["BOOTSTRAP_SERVERS"] = new(ParameterRequiredFormat.String, ParameterValueRequired.True, ParameterAllowMultipleValues.True)
        };

        public ProducerBuilderServersConfigurator(List<InnerBuilderParameter> parameters) : base(parameters) { }

        protected override void Configure<TKey, TValue>(ProducerBuilder<TKey, TValue> producerBuilder, FrozenSet<InnerBuilderParameter> parameters)
        {
            var bootstrapServers = parameters
                .Where(par => par.Key == "BOOTSTRAP_SERVERS")
                .Select(par => par.Value)
                .ToArray();

            producerBuilder.WithBootstrapServers(bootstrapServers);
        }
    }
}

