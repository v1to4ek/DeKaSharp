using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.BrokerClientCore.Tasks;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Registiry.Registries;
using DeKaSharp.Routing.Abstractions;

namespace DeKaSharp.BrokerClientCore.Configurators.TaskContainerProviders.Producer
{
    internal class DefaultProducerTaskContainerProvider : ProducerTaskContainerProviderBase, ITaskContainerProviderInstanceDescriptor<DefaultProducerTaskContainerProvider>
    {
        public static string Name => "DEFAULT_PRODUCER";

        public static DefaultProducerTaskContainerProvider Create(IDependencyProvider provider) => new(provider);

        public static IReadOnlyCollection<Type>? RequiredProviderRegistries => [typeof(RouterRegistry)];

        protected override IEnumerable<Enum>? RequiredStringParameters => [ProducerTaskContainerBaseParameters.Topic];

        protected override IEnumerable<Enum>? RequiredBooleanParameters => null;

        private readonly IManagedRouter router;

        public DefaultProducerTaskContainerProvider(IDependencyProvider registryProvider) : base(registryProvider) => router = registryProvider.GetDependentRegistryProvider<RouterRegistry, IRouterProvider>().GetForManaged();

        protected override ProducerTaskContainerBase<TKey, TValue> Build<TKey, TValue>(Guid id, IParametersSource parameters, ProducerBuilder<TKey, TValue> clientBuilder)
        {
            var client = clientBuilder
                .BuildAsync()
                .AsTask()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            var container = new ProducerTaskContainerBase<TKey, TValue>(id, parameters, client, router);

            return container;
        }
    }
}
