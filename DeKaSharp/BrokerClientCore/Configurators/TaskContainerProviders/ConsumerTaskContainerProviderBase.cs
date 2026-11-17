using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.BrokerClientCore.Parameters;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.BrokerClientCore.Tasks;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Registiry.Registries;

namespace DeKaSharp.BrokerClientCore.Configurators.TaskContainerProviders
{
    internal abstract class ConsumerTaskContainerProviderBase : IConsumerTaskContainerProvider, ITaskContainerProviderSource<IConsumerTaskContainerProvider>, ITaskContainerProviderClientDescriptor
    {
        ClientType ITaskContainerProvider.ClientType => ClientType;

        public static ClientType ClientType => ClientType.Producer;

        private static readonly Enum[] BaseBooleanRequiredParameters = [BaseClientParameter.ThrowExOnCancellation, BaseClientParameter.ThrowExOnError];

        private readonly IInitializedParamatersProvider _parametersProvider;

        private ParametersValidator? _parametersValidator;

        private bool _validated;

        protected abstract IEnumerable<Enum>? RequiredBooleanParameters { get; }

        protected abstract IEnumerable<Enum>? RequiredStringParameters { get; }

        protected ConsumerTaskContainerProviderBase(IDependencyProvider registryProvider)
        {
            _parametersProvider = registryProvider.GetIndependentRegistryProvider<ParametersRegistry, IInitializedParamatersProvider>();

            _validated = false;
        }

        IConsumerTaskContainerProvider ITaskContainerProviderSource<IConsumerTaskContainerProvider>.GetProvider()
        {
            if (_validated) return this;

            _parametersValidator = GetValidator(RequiredBooleanParameters, RequiredStringParameters);

            _validated = true;

            return this;
        }

        (bool success, ConsumerTaskContainerBase<TKey, TValue>? container, List<Enum>? errorParametersList) IConsumerTaskContainerProvider.CreateContainer<TKey, TValue>(Guid id, IParametersProvider parametersProvider, ConsumerBuilder<TKey, TValue> clientBuilder)
        {
            if (!_validated) throw new InvalidOperationException();

            var validator = _parametersValidator!;

            var parametersResult = parametersProvider.TryCreateParameters(validator);

            if (parametersResult.success == false) return (false, null, parametersResult.missingParameters);

            var parametersSourceProvider = parametersResult.provider!;

            var taskContainer = Build(id, parametersSourceProvider, clientBuilder);

            if (taskContainer.ClientType != ClientType) throw new ArgumentException();

            return (true, taskContainer, null);
        }

        protected abstract ConsumerTaskContainerBase<TKey, TValue> Build<TKey, TValue>
            (Guid id, IParametersSource parametersProvider, ConsumerBuilder<TKey, TValue> clientBuilder);

        private ParametersValidator GetValidator(IEnumerable<Enum>? requiredBooleanParameters, IEnumerable<Enum>? requiredStringParameters)
        {
            var booleanParameters = requiredBooleanParameters == null ? BaseBooleanRequiredParameters : BaseBooleanRequiredParameters.Concat(requiredBooleanParameters);

            var validator = new ParametersValidator(_parametersProvider, booleanParameters, requiredStringParameters);

            return validator;
        }
    }
}
