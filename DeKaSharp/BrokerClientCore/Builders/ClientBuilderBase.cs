using DeKaSharp.BrokerClientCore.Abstractions.Builders;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Abstractions.TaskContainers;
using DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Enums;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Builders
{
    internal abstract class ClientBuilderBase<TKafkaBuilder> : IBrokerClientBuilder
        where TKafkaBuilder : class
    {
        public Guid Id { get; private set; }

        public BuilderState BuilderState { get; private set; }

        public BuilderBehaviorType BehaviorType { get; }

        public abstract ClientType ClientType { get; }

        protected IParametersProvider clientParametersProvider;

        protected TKafkaBuilder clientBuilder;

        private readonly Func<TKafkaBuilder> _clientBuilderFactory;

        private readonly IRegistryProvider _registryProvider;

        protected ClientBuilderBase(Func<TKafkaBuilder> clientBuilderFactory, IRegistryProvider registryProvider, BuilderBehaviorType builderType)
        {
            _clientBuilderFactory = clientBuilderFactory;

            _registryProvider = registryProvider;

            clientParametersProvider = registryProvider.GetProvider<IParametersProvider>();

            clientBuilder = clientBuilderFactory(); 

            BehaviorType = builderType;

            BuilderState = BuilderState.Processing;

            Id = Guid.NewGuid();

            Logger.Log($"Создан строиель клиента c id: {Id}");
        }

        protected abstract void ConfigureBuilder(IBuilderConfigurator configurator);

        protected abstract void ConfigureParameters(IParameterConfigurator configurator);

        protected abstract void ConfigureTaskContainer(ITaskContainerConfigurator configurator);

        protected abstract void SetTaskContainerProvider(ITaskContainerProvider baseProvider);

        //добавить невозможность сборки без добавленных обязательных параметров-конфигураторов
        protected abstract IBrokerTaskContainer BuildBase();

        protected abstract void RebuildDependenciesBase();

        public void ConfigureWith(IBuilderConfigurator configurator)
        {
            if (BehaviorType == BuilderBehaviorType.Disposable && BuilderState == BuilderState.Builded) throw new InvalidOperationException();

            ValidateClientType(configurator.ClientType);

            ConfigureBuilder(configurator);

            Logger.Log($"Выполнен этап конфигурации билдера клиента с id: {Id} ");
        }

        public void ConfigureWith(IParameterConfigurator configurator)
        {
            if (BehaviorType == BuilderBehaviorType.Disposable && BuilderState == BuilderState.Builded) throw new InvalidOperationException();

            if (configurator.ClientType == ConfiguratorClientType.Universal)
            {
                ConfigureParameters(configurator);
            }
            else if (configurator.ClientType == ConfiguratorClientType.Consumer && ClientType == ClientType.Consumer)
            {
                ConfigureParameters(configurator);
            }
            else if (configurator.ClientType == ConfiguratorClientType.Producer && ClientType == ClientType.Producer)
            {
                ConfigureParameters(configurator);
            }
            else
            {
                throw new InvalidOperationException();
            }

            Logger.Log($"Выполнен этап конфигурации параметров клиента с id: {Id} ");
        }

        public void ConfigureWith(ITaskContainerConfigurator configurator)
        {
            if (BehaviorType == BuilderBehaviorType.Disposable && BuilderState == BuilderState.Builded) throw new InvalidOperationException();

            ValidateClientType(configurator.ClientType);

            ConfigureTaskContainer(configurator);

            Logger.Log($"Выполнен этап конфигурации задачи клиента с id: {Id} ");
        }

        public void ConfigureWith(ITaskContainerProvider provider)
        {
            if (BehaviorType == BuilderBehaviorType.Disposable && BuilderState == BuilderState.Builded) throw new InvalidOperationException();

            ValidateClientType(provider.ClientType);

            SetTaskContainerProvider(provider);

            Logger.Log($"Выполнен этап генерации задачи клиента с id: {Id} ");
        }

        public IBrokerTaskContainer Build()
        {
            if (BehaviorType == BuilderBehaviorType.Disposable && BuilderState == BuilderState.Builded) throw new InvalidOperationException();

            var container = BuildBase();

            if (BehaviorType == BuilderBehaviorType.Disposable)
            {
                BuilderState = BuilderState.Builded;

                return container;
            }

            if (BehaviorType == BuilderBehaviorType.Reusable)
            {
                RebuildDependencies();
            }

            return container;
        }

        private void RebuildDependencies()
        {
            Id = Guid.NewGuid();

            clientBuilder = _clientBuilderFactory();

            clientParametersProvider = _registryProvider.GetProvider<IParametersProvider>();

            RebuildDependenciesBase();
        }

        private void ValidateClientType(ClientType clientType)
        {
            if (clientType != ClientType) throw new ArgumentException($"Invalid client type. Expected: {ClientType}, but got: {clientType}.");
        }
    }
}
