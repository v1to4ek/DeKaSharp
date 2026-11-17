using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.BrokerClientCore.Parameters
{
    internal sealed class ParametersValidator 
    {
        private readonly HashSet<Enum>? _requiredBooleanParameters;

        private readonly HashSet<Enum>? _requiredStringParameters;

        public ParametersValidator(IInitializedParamatersProvider parametersProvider, IEnumerable<Enum>? requiredBooleanParameters, IEnumerable<Enum>? requiredStringParameters)
        {
            if (requiredBooleanParameters != null) _requiredBooleanParameters = [.. requiredBooleanParameters];

            if (requiredStringParameters != null) _requiredStringParameters = [.. requiredStringParameters];

            CheckRequiredParameters(parametersProvider);
        }

        private void CheckRequiredParameters(IInitializedParamatersProvider parametersProvider)
        {
            List<Enum>? undefinedBooleanParameters = null;

            List<Enum>? undefinedStringParameters = null;

            if (_requiredBooleanParameters != null && _requiredBooleanParameters.Count > 0)
            {
                var booleanParameters = parametersProvider.GetInitializedParameters<bool>();

                foreach (var parameter in _requiredBooleanParameters)
                {
                    if (!booleanParameters.ContainsKey(parameter))
                    {
                        undefinedBooleanParameters ??= [];

                        undefinedBooleanParameters.Add(parameter);
                    }
                }
            }

            if (_requiredStringParameters != null && _requiredStringParameters.Count > 0)
            {
                var stringParameters = parametersProvider.GetInitializedParameters<string>();

                foreach (var parameter in _requiredStringParameters)
                {
                    if (!stringParameters.ContainsKey(parameter))
                    {
                        undefinedStringParameters ??= [];

                        undefinedStringParameters.Add(parameter);
                    }
                }
            }

            if (undefinedStringParameters == null && undefinedBooleanParameters == null)
            {
                return;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public (bool success, List<Enum>? missing) ValidateBoolean(Dictionary<Enum, Parameter<bool>> booleanParameters)
        {
            if (_requiredBooleanParameters == null || _requiredBooleanParameters.Count == 0) return (true, null);

            List<Enum>? missingParameters = null;

            foreach (var parameter in _requiredBooleanParameters)
            {
                if (!booleanParameters.ContainsKey(parameter))
                {
                    missingParameters ??= [];

                    missingParameters.Add(parameter);
                }
            }

            if (missingParameters == null)
            {
                return (true, null);
            }
            else
            {
                return (false, missingParameters);
            }
        }

        public (bool success, List<Enum>? missing) ValidateString(Dictionary<Enum, Parameter<string>> stringParameters)
        {
            if (_requiredStringParameters == null || _requiredStringParameters.Count == 0) return (true, null);

            List<Enum>? missingParameters = null;

            foreach (var parameter in _requiredStringParameters)
            {
                if (!stringParameters.TryGetValue(parameter, out Parameter<string>? parameterValue))
                {
                    missingParameters ??= [];

                    missingParameters.Add(parameter);
                }
                else
                {
                    if (parameterValue.Value == null)
                    {
                        missingParameters ??= [];

                        missingParameters.Add(parameter);
                    }
                }
            }

            if (missingParameters == null)
            {
                return (true, null);
            }
            else
            {
                return (false, missingParameters);
            }
        }
    }
}
