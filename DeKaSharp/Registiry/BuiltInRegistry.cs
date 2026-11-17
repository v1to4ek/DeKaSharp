using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry
{
    internal delegate void ProviderSupplier(IDependencyProvider provider);

    internal delegate void ProviderSupplierCallback(ProviderSupplier providerSupplier);

    //возможно переработать логику хранения реестров в виде словаря, где ключ - тип реестра, а значение - экземпляр реестра.
    //Это позволит упростить поиск и доступ к реестрам по их типу, а также улучшить производительность при большом количестве реестров. 
    internal class BuiltInRegistry : IRegistryContainer, IDependentRegistryContainer, IRegistryProvider
    {
        private readonly List<IRegistry> _tempRegistries;

        private readonly HashSet<Type> _registeredRegistriyTypes;

        private FrozenSet<IRegistry>? _registriesContainer;

        private bool _isInitialized;

        private bool _initializationFailed;

        public BuiltInRegistry()
        {
            _tempRegistries = [];

            _registeredRegistriyTypes = [];

            _registriesContainer = null;

            _isInitialized = false;

            _initializationFailed = false;
        }

        void IRegistryContainer.Register<TRegistry, TProvider>(TRegistry registryInstance)
            => RegisterRegistryInstance(registryInstance, typeof(TRegistry));

        void IDependentRegistryContainer.Register<TRegistry, TProvider>(TRegistry registryInstance)
            => RegisterRegistryInstance(registryInstance, typeof(TRegistry));

        void IRegistryContainer.Register<TRegistry, TProvider>(Action<TRegistry> registryApplicator)
            => RegisterRegistryApplicator(registryApplicator);

        void IDependentRegistryContainer.Register<TRegistry, TProvider>(Action<TRegistry> registryApplicator)
            => RegisterRegistryApplicator(registryApplicator);

        private void RegisterRegistryInstance(IRegistry registryInstance, Type registryType)
        {
            if (_isInitialized) throw new InvalidOperationException();

            if (_tempRegistries.Contains(registryInstance)) throw new ArgumentException();

            EnsureSingleImplementation(registryInstance);

            _tempRegistries.Add(registryInstance);

            _registeredRegistriyTypes.Add(registryType);
        }

        private void RegisterRegistryApplicator<TRegistry>(Action<TRegistry> registryApplicator)
            where TRegistry : class, IRegistry, new()
        {
            if (_isInitialized) throw new InvalidOperationException();

            var registryType = typeof(TRegistry);

            if (_registeredRegistriyTypes.Contains(registryType))
            {
                var registry = _tempRegistries.FirstOrDefault(r => r.GetType() == registryType) ?? throw new Exception();

                registryApplicator((TRegistry)registry);
            }
            else
            {
                var registry = new TRegistry();

                EnsureSingleImplementation(registry);

                registryApplicator(registry);

                _tempRegistries.Add(registry);

                _registeredRegistriyTypes.Add(registryType);
            }
        }

        public IRegistryProvider InitializeProvider()
        {
            if (_isInitialized) return this;

            if (_initializationFailed)
                throw new InvalidOperationException("Предыдущая инициализация завершилась ошибкой");

            if (_tempRegistries.Count == 0) throw new InvalidOperationException();

            try
            {
                InitializeBase();
            }
            catch
            {
                _initializationFailed = true;
                throw;
            }

            return this;
        }

        public TProvider GetProvider<TProvider>() where TProvider : IDataProvider
        {
            if (!_isInitialized) throw new InvalidOperationException();

            foreach(var registry in _registriesContainer!)
            {
                if (registry is IProviderSupplier<TProvider> supplier)
                {
                    var provider = supplier.GetProvider();

                    return provider;
                }
            }

            throw new ArgumentException();
        }

        private void InitializeBase()
        {
            var tempIndependentRegistries = _tempRegistries
                .Where(reg => reg is IInitializableRegistry)
                .Cast<IInitializableRegistry>()
                .ToList();

            var tempDependentRegistries = _tempRegistries
                .Where(reg => reg is IDependentInitializableRegistry)
                .Cast<IDependentInitializableRegistry>()
                .ToList();

            ValidateDependencies(tempDependentRegistries);

            foreach (var registry in tempIndependentRegistries)
            {
                registry.Initialize();
            }

            Dictionary<IDependentInitializableRegistry, ProviderSupplier> providerSupplierCallbacks = [];

            foreach (var registry in tempDependentRegistries)
            {
                registry.Initialize(callback
                    => providerSupplierCallbacks[registry] = providerSupplierCallbacks.TryGetValue(registry, out var existingCallback)
                    ? existingCallback + callback
                    : callback);
            }

            _registriesContainer = _tempRegistries.ToFrozenSet();

            var orderedRegistries = OrderByDependencies(tempDependentRegistries);

            var builtRegistries = new HashSet<IDependentInitializableRegistry>();

            foreach (var registry in orderedRegistries)
            {
                if (providerSupplierCallbacks.TryGetValue(registry, out var callback))
                {
                    callback(new DependencyProviderScope(this, registry, builtRegistries));
                }

                builtRegistries.Add(registry);
            }

            _tempRegistries.Clear();

            _isInitialized = true;
        }

        private static void EnsureSingleImplementation(IRegistry registry)
        {
            if (registry is IInitializableRegistry && registry is IDependentInitializableRegistry)
            {
                throw new InvalidOperationException($"{registry.GetType().Name} реализует и IInitializableRegistry, и IDependentInitializableRegistry; Реализация обоих интерфейсов невозможна");
            }
        }

        private static void ValidateDependencies(List<IDependentInitializableRegistry> dependentRegistries)
        {
            var dependentTypes = dependentRegistries.Select(reg => reg.GetType()).ToHashSet();

            foreach (var registry in dependentRegistries)
            {
                if (registry.RequiredDependentRegistries == null) continue;

                foreach (var dependency in registry.RequiredDependentRegistries)
                {
                    if (!dependentTypes.Contains(dependency))
                    {
                        throw new InvalidOperationException(
                            $"{registry.GetType().Name} объявил зависимость от {dependency.Name}, но среди зависимых реестров такого нет " +
                            "(он не зарегистрирован или он независимый: для него используйте GetDependencyProvider)");
                    }
                }
            }
        }

        private static IReadOnlyCollection<Type> GetDependencies(IDependentInitializableRegistry registry) => registry.RequiredDependentRegistries ?? Array.Empty<Type>();

        //алгоритм Кана
        private static List<IDependentInitializableRegistry> OrderByDependencies(List<IDependentInitializableRegistry> sourceRegistries)
        {
            var ordered = new List<IDependentInitializableRegistry>(sourceRegistries.Count);

            var remaining = sourceRegistries.ToList();

            while (remaining.Count > 0)
            {
                var index = remaining
                    .FindIndex(candidate =>
                    !GetDependencies(candidate)
                    .Any(dep => remaining
                    .Any(other => other
                    .GetType() == dep)));

                if (index < 0)
                {
                    throw new InvalidOperationException("Циклическая зависимость между реестрами: " + string.Join(", ", remaining.Select(reg => reg.GetType().Name)));
                }

                ordered.Add(remaining[index]);

                remaining.RemoveAt(index);
            }

            return ordered;
        }

        private sealed class DependencyProviderScope : IDependencyProvider
        {
            private readonly IDependentInitializableRegistry _registryConsumer;

            private readonly FrozenSet<IRegistry> _sourceRegistries;

            private readonly IReadOnlyCollection<Type>? _requiredRegistries;

            private readonly HashSet<IDependentInitializableRegistry> _initializedRegistries;

            public DependencyProviderScope(BuiltInRegistry registryProvider, IDependentInitializableRegistry registryConsumer, HashSet<IDependentInitializableRegistry> initializedRegistries)
            {
                _registryConsumer = registryConsumer;

                _sourceRegistries = registryProvider._registriesContainer ?? throw new InvalidOperationException();

                _requiredRegistries = registryConsumer.RequiredDependentRegistries;

                _initializedRegistries = initializedRegistries;
            }

            TProvider IDependencyProvider.GetDependentRegistryProvider<TRegistry, TProvider>()
            {
                if (_requiredRegistries == null) throw new InvalidOperationException();

                if (!_requiredRegistries.Contains(typeof(TRegistry)))
                {
                    throw new InvalidOperationException($"{_registryConsumer.GetType().Name} запросил {typeof(TRegistry).Name}, но не объявил его в Dependencies");
                }

                var registry = _sourceRegistries.OfType<TRegistry>().FirstOrDefault() ?? throw new ArgumentException($"Реестр {typeof(TRegistry).Name} не зарегистрирован");

                if (!_initializedRegistries.Contains(registry))
                {
                    throw new InvalidOperationException($"Провайдер {typeof(TRegistry).Name} ещё не построен");
                }

                return registry.GetProvider();
            }

            TProvider IDependencyProvider.GetIndependentRegistryProvider<TRegistry, TProvider>()
            {
                var registry = _sourceRegistries.OfType<TRegistry>().FirstOrDefault() ?? throw new ArgumentException($"Реестр {typeof(TRegistry).Name} не зарегистрирован");

                return registry.GetProvider();
            }
        }
    }
}
 