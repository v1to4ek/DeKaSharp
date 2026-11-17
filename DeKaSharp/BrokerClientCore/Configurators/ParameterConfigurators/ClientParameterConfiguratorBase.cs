using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators.Enums;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Registiry.Registries;

namespace DeKaSharp.BrokerClientCore.Configurators.ParameterConfigurators
{
    internal abstract class ClientParameterConfiguratorBase<TValue> : IParameterConfigurator, IParameterConfiguratorSource
    {
        ConfiguratorClientType IParameterConfigurator.ClientType => ParseClientType(ClientType);

        public abstract Enum ParameterEnum { get; }

        protected abstract ClientType ClientType { get; }

        private bool _isInitialized;

        private TValue? _outputParameterValue;

        private readonly IInitializedParamatersProvider _initializedParamaters;

        protected ClientParameterConfiguratorBase(IDependencyProvider registryProvider)
        {
            _initializedParamaters = registryProvider.GetIndependentRegistryProvider<ParametersRegistry, IInitializedParamatersProvider>();

            try
            {
                _initializedParamaters.GetInitializedParameters<TValue>();
            }
            catch (Exception)
            {
                throw new ArgumentException();
            }

            _isInitialized = false;
        }

        IParameterConfigurator IParameterConfiguratorSource.GetConfigurator(string parameterValue)
        {
            if (_isInitialized) return this;

            var initializedParamaters = _initializedParamaters.GetInitializedParameters<TValue>();

            if (!initializedParamaters.ContainsKey(ParameterEnum)) throw new ArgumentException();

            _outputParameterValue = ParseValue(parameterValue);

            _isInitialized = true;

            return this;
        }

        void IParameterConfigurator.Configure(IParametersProvider parametersProvider)
        {
            if (!_isInitialized) throw new InvalidOperationException();

            parametersProvider.SetParameter(ParameterEnum, _outputParameterValue);
        }

        private static ConfiguratorClientType ParseClientType(ClientType clientType)
        {
            if (clientType == ClientType.Producer) return ConfiguratorClientType.Producer;

            if (clientType == ClientType.Consumer) return ConfiguratorClientType.Consumer;

            else throw new ArgumentException();
        }

        private static TValue ParseValue(string stringValue)
        {
            if (typeof(TValue) == typeof(bool))
            {
                if (stringValue == "TRUE") return (TValue)(object)true;

                if (stringValue == "FALSE") return (TValue)(object)false;

                throw new ArgumentException();
            }

            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)stringValue;
            }

            throw new ArgumentException();
        }
    }
}
