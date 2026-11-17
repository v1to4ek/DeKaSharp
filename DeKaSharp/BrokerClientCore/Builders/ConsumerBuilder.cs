using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Abstractions.TaskContainers;
using DeKaSharp.BrokerClientCore.Configurators.TaskContainerProviders.Consumer;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Builders
{
    internal class ConsumerBuilder<TKey, TValue>
        : ClientBuilderBase<Dekaf.ConsumerBuilder<TKey, TValue>>
    {
        public override ClientType ClientType => ClientType.Consumer;

        private readonly List<ITaskContainerConfigurator> _taskContainerConfigurators;

        private readonly IRegistryProvider _registryProvider;

        private IConsumerTaskContainerProvider _taskContainerProvider;

        public ConsumerBuilder(BuilderBehaviorType builderType, IRegistryProvider registryProvider)
            : base(Kafka.CreateConsumer<TKey, TValue>, registryProvider, builderType) 
        {
            _taskContainerConfigurators = [];

            _registryProvider = registryProvider;

            _taskContainerProvider = new DefaultConsumerTaskContainerProvider(registryProvider);
        }

        //Все методы ConfigureWithBase можно вынести в базовый класс
        protected override void ConfigureBuilder(IBuilderConfigurator configurator)
        {
            var builderConfigurator = configurator as IConsumerBuilderConfigurator ?? throw new ArgumentException();

            builderConfigurator.Configure(clientBuilder);
        }

        protected override void ConfigureParameters(IParameterConfigurator configurator)
        {
            configurator.Configure(clientParametersProvider);
        }

        protected override void ConfigureTaskContainer(ITaskContainerConfigurator configurator)
        {
            _taskContainerConfigurators.Add(configurator);
        }

        protected override void SetTaskContainerProvider(ITaskContainerProvider baseProvider)
        {
            var provider = baseProvider as IConsumerTaskContainerProvider ?? throw new ArgumentException();

            _taskContainerProvider = provider;
        }

        protected override IBrokerTaskContainer BuildBase()
        {
            var creationResult = _taskContainerProvider.CreateContainer(Id, clientParametersProvider, clientBuilder);

            if (!creationResult.success)
            {
                throw new ArgumentException();
            }

            var container = creationResult.container!;

            foreach (var configurator in _taskContainerConfigurators)
            {
                configurator.Configure(container);
            }

            return container;
        }

        protected override void RebuildDependenciesBase()
        {
            _taskContainerConfigurators.Clear();

            _taskContainerProvider = new DefaultConsumerTaskContainerProvider(_registryProvider);
        }
    }
}
