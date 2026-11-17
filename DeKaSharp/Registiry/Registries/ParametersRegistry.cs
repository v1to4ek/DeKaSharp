using DeKaSharp.BrokerClientCore.Parameters;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Registries
{
    internal class ParametersRegistry : IInitializableRegistry, IProviderSupplier<IParametersProvider>, IProviderSupplier<IInitializedParamatersProvider>
    {
        private class InitializedParameters : IInitializedParamatersProvider
        {
            private readonly FrozenDictionary<Enum, Parameter<bool>> _booleanParameters;

            private readonly FrozenDictionary<Enum, Parameter<string>> _stringParameters;

            public InitializedParameters(FrozenDictionary<Enum, Parameter<bool>> booleanParameters, FrozenDictionary<Enum, Parameter<string>> stringParameters)
            {
                _booleanParameters = booleanParameters;

                _stringParameters = stringParameters;
            }

            public IReadOnlyDictionary<Enum, Parameter<TValue>> GetInitializedParameters<TValue>()
            {
                if (typeof(TValue) == typeof(bool))
                {
                    return (IReadOnlyDictionary<Enum, Parameter<TValue>>)_booleanParameters;
                }

                if (typeof(TValue) == typeof(string))
                {
                    return (IReadOnlyDictionary<Enum, Parameter<TValue>>)_stringParameters;
                }

                throw new NotSupportedException();
            }
        }

        public string RegistryName => nameof(ParametersRegistry);

        private readonly HashSet<Enum> _registeredEnums;

        private readonly Dictionary<Enum, Parameter<bool>> _booleanParameters;

        private readonly Dictionary<Enum, Parameter<string>> _stringParameters;

        private IInitializedParamatersProvider? _initializedParamatersProvider;

        private bool _isInitialized;

        public ParametersRegistry()
        {
            _isInitialized = false;

            _registeredEnums = [];

            _booleanParameters = [];

            _stringParameters = [];
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            List<Enum>? unregistered = null;

            foreach (var e in _registeredEnums)
            {
                if (!_booleanParameters.ContainsKey(e) && !_stringParameters.ContainsKey(e))
                {
                    unregistered ??= [];

                    unregistered.Add(e);
                }
            }

            if (unregistered != null)
            {
                throw new InvalidOperationException();
            }

            _initializedParamatersProvider = new InitializedParameters(_booleanParameters.ToFrozenDictionary(), _stringParameters.ToFrozenDictionary());

            _isInitialized = true;
        }

        public void RegisterParametersEnum<T>() where T : struct, Enum
        {
            if (_isInitialized) throw new InvalidOperationException();

            var enumValues = Enum.GetValues<T>();

            foreach (var value in enumValues)
            {
                _registeredEnums.Add(value);
            }
        }

        public void RegisterParameter<TValue>(Parameter<TValue> parameter)
        {
            if (_isInitialized) throw new InvalidOperationException();

            if (_booleanParameters.ContainsKey(parameter.Name) || _stringParameters.ContainsKey(parameter.Name))
            {
                throw new InvalidOperationException();
            }

            if (typeof(TValue) == typeof(bool))
            {
                var boolParameter = parameter as Parameter<bool> ?? throw new ArgumentException();

                var name = boolParameter.Name;

                _booleanParameters.Add(name, boolParameter);

                return;
            }

            if (typeof(TValue) == typeof(string))
            {
                var stringParameter = parameter as Parameter<string> ?? throw new ArgumentException();

                var name = stringParameter.Name;

                _stringParameters.Add(name, stringParameter);

                return;
            }

            throw new ArgumentException();
        }

        private static Dictionary<Enum, Parameter<T>> Clone<T>(IReadOnlyDictionary<Enum, Parameter<T>> source)
        {
            var copy = new Dictionary<Enum, Parameter<T>>(source.Count);

            foreach (var (key, parameter) in source)
                copy.Add(key, parameter with { });

            return copy;
        }

        IParametersProvider IProviderSupplier<IParametersProvider>.GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            var booleanParameters = Clone(_booleanParameters);

            var stringParameters = Clone(_stringParameters);

            return new ParametersContainer(booleanParameters, stringParameters);
        }

        IInitializedParamatersProvider IProviderSupplier<IInitializedParamatersProvider>.GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _initializedParamatersProvider ?? throw new InvalidOperationException();
        }
    }
}
