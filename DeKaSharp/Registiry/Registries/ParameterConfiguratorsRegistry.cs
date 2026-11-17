using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Registries
{
    internal class ParameterConfiguratorsRegistry : IDependentInitializableRegistry, IProviderSupplier<IParameterConfiguratorProvider>
    {
        private sealed class ParameterConfiguratorProvider : IParameterConfiguratorProvider
        {
            private readonly FrozenDictionary<string, Func<string, IParameterConfigurator>> _configuratorFactories;

            public ParameterConfiguratorProvider(FrozenDictionary<string, Func<string, IParameterConfigurator>> configuratorFactories)
            {
                _configuratorFactories = configuratorFactories;
            }

            public IParameterConfigurator GetConfigurator(string configuratorName, string parameterValue)
            {
                var factory = _configuratorFactories[configuratorName];

                var configurator = factory(parameterValue);

                return configurator;
            }
        }

        public string RegistryName => nameof(ParameterConfiguratorsRegistry);

        private readonly record struct ConfiguratorSourceFactory(string Name, Func<IDependencyProvider, IParameterConfiguratorSource> SourceFactory);

        private readonly List<ConfiguratorSourceFactory> _sourceFactories;

        private IParameterConfiguratorProvider? _parameterConfiguratorProvider;

        private bool _isInitialized;

        public ParameterConfiguratorsRegistry()
        {
            _isInitialized = false;

            _parameterConfiguratorProvider = null;

            _sourceFactories = [];
        }

        public void Initialize(ProviderSupplierCallback providerSupplierCallback)
        {
            if (_isInitialized) return;

            if (_sourceFactories.Count == 0) throw new InvalidOperationException();

            ValidateSourceFactories(_sourceFactories);

            Dictionary<string, Func<string, IParameterConfigurator>> configuratorFactories = [];

            providerSupplierCallback(provider =>
            {
                foreach (var factory in _sourceFactories)
                {
                    var name = factory.Name;

                    var source = factory.SourceFactory(provider);

                    configuratorFactories.Add(name, parameter => source.GetConfigurator(parameter));
                }

                _parameterConfiguratorProvider = new ParameterConfiguratorProvider(configuratorFactories.ToFrozenDictionary());
            });

            _isInitialized = true;
        }

        private static void ValidateSourceFactories(List<ConfiguratorSourceFactory> sourceFactories)
        {
            Dictionary<string, int> names = [];

            foreach(var factory in sourceFactories)
            {
                var name = factory.Name;

                if (!names.TryAdd(name, 1))
                {
                    names[name] += 1;
                }
            }

            var repeatedNames = names
                .Where(kv => kv.Value > 1)
                .Select(kv => $"{kv.Key} ({kv.Value})")
                .ToList();

            if (repeatedNames.Count == 0) return;

            throw new ArgumentException($"Duplicated names found: {string.Join(", ", repeatedNames)}");
        }

        public void Register<TConfigurtor>()
            where TConfigurtor :
            IParameterConfigurator,
            IParameterConfiguratorSource,
            IParameterConfiguratorDescriptor<TConfigurtor>
        {
            if (_isInitialized) throw new InvalidOperationException();

            var key = TConfigurtor.Name;

            _sourceFactories.Add(new ConfiguratorSourceFactory(key, static registryProvider => TConfigurtor.Create(registryProvider)));
        }

        public IParameterConfiguratorProvider GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _parameterConfiguratorProvider!;
        }
    }
}
