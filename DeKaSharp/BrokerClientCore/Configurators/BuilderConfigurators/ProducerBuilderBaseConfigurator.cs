using Dekaf;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Enums;
using System.Collections.Frozen;

namespace DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators
{
    //СДЕЛАНО
    //возможно на всякий случай стоит разделить IBuilderConfiguratorSource на 2 интерфейса:
    //один для получения IBuilderConfigurator а другой для получения статического свойства типа клиента ClientType.
    internal abstract class ProducerBuilderBaseConfigurator
        : IProducerBuilderConfigurator, IBuilderConfiguratorSource<IProducerBuilderConfigurator>, IBuilderConfiguratorClientDescriptor
    {
        ClientType IBuilderConfigurator.ClientType => ClientType;

        public static ClientType ClientType => ClientType.Producer;

        private readonly FrozenSet<InnerBuilderParameter> _parameters;

        private bool _validated;

        protected abstract Dictionary<string, ParameterRequirement> ParameterRequierments { get; }

        protected ProducerBuilderBaseConfigurator(List<InnerBuilderParameter> parameters)
        {
            //полностью одинаковые параметры (и ключ и значение одинаковые) схлопываются в один
            _parameters = parameters.ToFrozenSet();

            _validated = false;
        }

        IProducerBuilderConfigurator IBuilderConfiguratorSource<IProducerBuilderConfigurator>.GetBuilderConfigurator()
        {
            if (_validated) return this;

            ValidateParametersBase(_parameters);

            ValidateParameters(_parameters);

            _validated = true;

            return this;
        }

        private void ValidateParametersBase(FrozenSet<InnerBuilderParameter> parameters)
        {
            var requirements = ParameterRequierments;

            var missedKeys = requirements.Keys.ToHashSet();

            var countedFoundKeys = requirements.Keys.ToDictionary(key => key, _ => 0);

            List<(string key, string value)>? errorParsedParams = null;

            foreach (var parameter in parameters)
            {
                if (!requirements.ContainsKey(parameter.Key))
                {
                    continue;
                }
                else
                {
                    missedKeys.Remove(parameter.Key);
                }

                if (!countedFoundKeys.TryAdd(parameter.Key, 1))
                {
                    countedFoundKeys[parameter.Key] += 1;
                }

                var parsed = BuilderParametersValidatorHelper.CheckValue(parameter, requirements);

                if (parsed == false)
                {
                    errorParsedParams ??= [];

                    errorParsedParams.Add((parameter.Key, parameter.Value));
                }
            }

            BuilderParametersValidatorHelper.CheckMissedKeys(missedKeys, requirements);

            BuilderParametersValidatorHelper.CheckMultipleKeys(countedFoundKeys, requirements);

            if (errorParsedParams != null) throw new ArgumentException();
        }

        protected virtual void ValidateParameters(FrozenSet<InnerBuilderParameter> parameters) { }

        void IProducerBuilderConfigurator.Configure<TKey, TValue>(ProducerBuilder<TKey, TValue> producerBuilder)
        {
            if (!_validated)
            {
                throw new InvalidOperationException("Parameters must be validated before configuring the producer builder.");
            }

            Configure(producerBuilder, _parameters);
        }

        protected abstract void Configure<TKey, TValue>(ProducerBuilder<TKey, TValue> producerBuilder, FrozenSet<InnerBuilderParameter> parameters);
    }
}
