using DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Registries
{
    internal interface ITaskContainerProvidersProvider : IDataProvider
    {

    }

    internal class TaskContainerProvidersRegistry : IDependentInitializableRegistry, IProviderSupplier<ITaskContainerProvidersProvider>
    {
        private readonly record struct ProviderRegistration(ClientType ProviderClient, Type ProviderType, string ProviderName, Func<IDependencyProvider, ITaskContainerProviderSource<ITaskContainerProvider>> SourceFactory);

        private readonly record struct ConstraintRegistraton(ClientType ConstraintClient, Type ConstraintInterfaceType, Type ConstraintSourceInterfaceType);

        public string RegistryName => nameof(TaskContainerProvidersRegistry);

        public IReadOnlyCollection<Type>? RequiredDependentRegistries;

        private readonly List<ProviderRegistration> _providerRegistrations;

        private readonly List<ConstraintRegistraton> _constraintRegistratons;

        private HashSet<Type>? _registriesRequirementsBuffer;

        private bool _isInitialized;

        public TaskContainerProvidersRegistry()
        {
            _isInitialized = false;

            RequiredDependentRegistries = null;

            _registriesRequirementsBuffer = null;

            _providerRegistrations = [];

            _constraintRegistratons = [];
        }

        public void Initialize(ProviderSupplierCallback supplierCallback)
        {
            if (_isInitialized) return;

            if (_providerRegistrations.Count == 0 || _constraintRegistratons.Count == 0) throw new InvalidOperationException();

            if (_registriesRequirementsBuffer != null) RequiredDependentRegistries = _registriesRequirementsBuffer;

        }

        public void Register<TProvider>()
            where TProvider:
            ITaskContainerProvider,
            ITaskContainerProviderSource<ITaskContainerProvider>,
            ITaskContainerProviderClientDescriptor,
            ITaskContainerProviderInstanceDescriptor<TProvider>
        {
            if (_isInitialized) throw new InvalidOperationException();

            var providerClient = TProvider.ClientType;

            var providerName = TProvider.Name;

            var providerType = typeof(TProvider);

            var providerRegistryRequirements = TProvider.RequiredProviderRegistries;

            if (providerRegistryRequirements != null) AddRegistryRequirements(providerRegistryRequirements);

            _providerRegistrations.Add(new ProviderRegistration(providerClient, providerType, providerName, static dependencyProvider => TProvider.Create(dependencyProvider)));
        }

        public void RegisterProviderConstraint<TConstraint>(ClientType constraintClient)
            where TConstraint : class, ITaskContainerProvider
        {
            if (_isInitialized) throw new InvalidOperationException();

            var constraintInterfaceType = typeof(TConstraint);

            var constraintSourceInterfaceType = typeof(ITaskContainerProviderSource<TConstraint>);

            _constraintRegistratons.Add(new ConstraintRegistraton(constraintClient, constraintInterfaceType, constraintSourceInterfaceType));
        }

        public ITaskContainerProvidersProvider GetProvider() 
        {
            if (!_isInitialized) throw new InvalidOperationException();

        }

        private void AddRegistryRequirements(IReadOnlyCollection<Type> requirements)
        {
            _registriesRequirementsBuffer ??= [];

            foreach (var requirement in requirements)
            {
                _registriesRequirementsBuffer.Add(requirement);
            }
        }
    }
}
