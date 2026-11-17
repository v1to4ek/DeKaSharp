using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Parameters
{
    internal class ParametersContainer : IParametersProvider, IParametersSource
    {
        private readonly Dictionary<Enum, Parameter<bool>> _booleanParameters;

        private readonly Dictionary<Enum, Parameter<string>> _stringParameters;

        public ParametersContainer(Dictionary<Enum, Parameter<bool>> booleanParameters, Dictionary<Enum, Parameter<string>> stringParameters)
        {
            _booleanParameters = booleanParameters;

            _stringParameters = stringParameters;
        }

        public void SetParameter<TValue>(Enum parameterName, TValue parameterValue)
        {
            if (_booleanParameters.TryGetValue(parameterName, out Parameter<bool>? valueBool) && parameterValue is bool boolValue)
            {
                valueBool.Value = boolValue;

                return;
            }

            if (_stringParameters.TryGetValue(parameterName, out Parameter<string>? valueString) && parameterValue is string stringValue)
            {
                valueString.Value = stringValue;

                return;
            }

            throw new ArgumentException();
        }

        public (bool success, IParametersSource? provider, List<Enum>? missingParameters) TryCreateParameters(ParametersValidator validator)
        {
            List<Enum>? missing = null;

            var boolResult = validator.ValidateBoolean(_booleanParameters);

            var stringResult = validator.ValidateString(_stringParameters);

            if (!boolResult.success)
            {
                missing ??= [];
                missing.AddRange(boolResult.missing!);
            }

            if (!stringResult.success)
            {
                missing ??= [];
                missing.AddRange(stringResult.missing!);
            }

            if (missing != null)
            {
                return (false, null, missing);
            }
            else
            {
                return (true, this, null);
            }
        }

        public TValue GetParameter<TValue>(Enum parameterName)
        {
            if (typeof(TValue) == typeof(bool))
            {
                if (_booleanParameters.TryGetValue(parameterName, out var value))
                {
                    return (TValue)(object)value.Value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            if (typeof(TValue) == typeof(string))
            {
                if (_stringParameters.TryGetValue(parameterName, out var value))
                {
                    ArgumentException.ThrowIfNullOrEmpty(value.Value, nameof(value.Value));

                    ArgumentException.ThrowIfNullOrWhiteSpace(value.Value, nameof(value.Value));

                    return (TValue)(object)value.Value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            throw new ArgumentException();
        }
    }
}
