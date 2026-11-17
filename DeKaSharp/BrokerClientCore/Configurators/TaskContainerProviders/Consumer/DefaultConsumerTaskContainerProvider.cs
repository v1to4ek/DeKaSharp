using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Tasks;
using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Configurators.TaskContainerProviders.Consumer
{
    internal class DefaultConsumerTaskContainerProvider : ConsumerTaskContainerProviderBase, ITaskContainerProviderInstanceDescriptor<DefaultConsumerTaskContainerProvider>
    {
        public static string Name => "DEFAULT_CONSUMER";

        public static DefaultConsumerTaskContainerProvider Create(IDependencyProvider provider) => new(provider);

        public DefaultConsumerTaskContainerProvider(IDependencyProvider registryProvider) : base(registryProvider) { }

        protected override IEnumerable<Enum>? RequiredBooleanParameters => null;

        protected override IEnumerable<Enum>? RequiredStringParameters => null;

        protected override ConsumerTaskContainerBase<TKey, TValue> Build<TKey, TValue>(Guid id, IParametersSource parameters, ConsumerBuilder<TKey, TValue> clientBuilder)
        {
            var client = clientBuilder
                .BuildAsync()
                .AsTask()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            var container = new ConsumerTaskContainerBase<TKey, TValue>(id, parameters, client);

            return container;
        }
    }
}
