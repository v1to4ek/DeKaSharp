using DeKaSharp.BrokerClientCore.Abstractions;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.BuilderConfigurators;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Registries
{
    internal class BuilderConfiguratorsRegistry : IInitializableRegistry, IProviderSupplier<IBuilderConfiguratorsProvider>
    {
        private class BuilderConfiguratorsProvider : IBuilderConfiguratorsProvider
        {
            private readonly FrozenDictionary<(ClientType configuratorClient, string configuratorName), ConfiguratorSourceFactory> _frozenConfiguratorFactories;

            public BuilderConfiguratorsProvider(FrozenDictionary<(ClientType configuratorClient, string configuratorName), ConfiguratorSourceFactory> frozenConfiguratorFactories)
            {
                _frozenConfiguratorFactories = frozenConfiguratorFactories;
            }

            public IBuilderConfigurator CreateConfigurator(string configuratorName, ClientType clientType, List<InnerBuilderParameter> builderParameters)
            {
                ArgumentNullException.ThrowIfNull(configuratorName);
                ArgumentNullException.ThrowIfNull(builderParameters);

                var factoryKey = (clientType, configuratorName);

                if (!_frozenConfiguratorFactories.TryGetValue(factoryKey, out var configuratorSourceFactory))
                {
                    throw new ArgumentException($"No configurator registered for {clientType} with name '{configuratorName}'.");
                }

                var configuratorSource = configuratorSourceFactory(builderParameters);

                var configurator = configuratorSource.GetBuilderConfigurator();

                if (configurator.ClientType != clientType)
                {
                    throw new InvalidOperationException($"Configurator '{configuratorName}' for {clientType} has mismatched client type: {configurator.ClientType}.");
                }

                return configurator;
            }
        }

        private readonly record struct ConfiguratorRegistration(ClientType ConfiguratorClient, Type ConfuguratorType, string ConfiguratorName, ConfiguratorSourceFactory ConfiguratorSourceFactory);

        private readonly record struct ConstraintRegistration(ClientType ConstraintClient, Type ConstraintInterfaceType, Type ConstraintSourceInterfaceType);

        private delegate IBuilderConfiguratorSource<IBuilderConfigurator> ConfiguratorSourceFactory(List<InnerBuilderParameter> parameters);

        public string RegistryName => nameof(BuilderConfiguratorsRegistry);

        private readonly List<ConfiguratorRegistration> _configuratorRegistrations;

        private readonly List<ConstraintRegistration> _constraintRegistrations;

        private IBuilderConfiguratorsProvider? _builderConfiguratorsProvider;

        private bool _isInitialized;

        public BuilderConfiguratorsRegistry()
        {
            _isInitialized = false;

            _builderConfiguratorsProvider = null;

            _configuratorRegistrations = [];

            _constraintRegistrations = [];
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            if (_configuratorRegistrations.Count == 0 || _constraintRegistrations.Count == 0) throw new InvalidOperationException();

            var constraintValidationResult = ValidateConstraints(_constraintRegistrations);

            if (constraintValidationResult.success == false) throw new ArgumentException();

            var constraints = constraintValidationResult.constraints!;

            var configuratorValidationResult = ValidateConfigurators(_configuratorRegistrations, constraints);

            if (configuratorValidationResult.success == false) throw new ArgumentException();

            var configuratorFactories = configuratorValidationResult.configuratorFactories!;

            _builderConfiguratorsProvider = new BuilderConfiguratorsProvider(configuratorFactories.ToFrozenDictionary());

            _configuratorRegistrations.Clear();

            _constraintRegistrations.Clear();

            _isInitialized = true;
        }

        public void Register<TConfigurator>() 
            where TConfigurator : class,
            IBuilderConfiguratorInstanceDescriptor<TConfigurator>, 
            IBuilderConfiguratorClientDescriptor, 
            IBuilderConfiguratorSource<IBuilderConfigurator>, 
            IBuilderConfigurator
        {
            if (_isInitialized) throw new InvalidOperationException();

            var configuratorClient = TConfigurator.ClientType;

            var configuratorName = TConfigurator.Name;

            var configuratorType = typeof(TConfigurator);

            _configuratorRegistrations.Add(new ConfiguratorRegistration(configuratorClient, configuratorType, configuratorName, static parameters => TConfigurator.Create(parameters)));
        }

        public void RegisterBuilderConstraint<TConstraint>(ClientType constraintClient)
            where TConstraint : class, IBuilderConfigurator
        {
            if (_isInitialized) throw new InvalidOperationException();

            var constraintInterfaceType = typeof(TConstraint);

            var constraintSourceInterfaceType = typeof(IBuilderConfiguratorSource<TConstraint>);

            _constraintRegistrations.Add(new ConstraintRegistration(constraintClient, constraintInterfaceType, constraintSourceInterfaceType));
        }

        public IBuilderConfiguratorsProvider GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _builderConfiguratorsProvider!;
        }

        private static (bool success, Dictionary<(ClientType configuratorClient, string configuratorName), ConfiguratorSourceFactory>? configuratorFactories, List<string>? errorMessages) ValidateConfigurators
            (List<ConfiguratorRegistration> configuratorRegistrations, Dictionary<ClientType, ConstraintRegistration> constraintRegistrations)
        {
            List<string>? errors = null;

            Dictionary<(ClientType configuratorClient, string configuratorName), ConfiguratorSourceFactory> configuratorFactories = [];

            Dictionary<(ClientType configuratorClient, string configuratorName), Type> owners = [];

            void AddError(string errorMessage)
            {
                errors ??= [];

                errors.Add(errorMessage);
            }

            foreach (var configurator in configuratorRegistrations)
            {
                var configuratorName = configurator.ConfiguratorName;

                var configuratorClient = configurator.ConfiguratorClient;

                var configuratorTypeName = configurator.ConfuguratorType.Name;

                if (string.IsNullOrWhiteSpace(configuratorName))
                {
                    AddError($"Configurator {configuratorTypeName} has an empty name.");
                    continue;
                }

                if (configurator.ConfuguratorType.IsAbstract)
                {
                    AddError($"{configuratorTypeName}: abstract type cannot be registered.");
                    continue;
                }

                if (!constraintRegistrations.TryGetValue(configurator.ConfiguratorClient, out var constraint))
                {
                    AddError($"{configuratorTypeName}: no constraint registered for {configuratorClient}.");
                    continue;
                }
                else if (!constraint.ConstraintSourceInterfaceType.IsAssignableFrom(configurator.ConfuguratorType))
                {
                    AddError($"{configuratorTypeName} must implement IBuilderConfiguratorSource<{constraint.ConstraintInterfaceType.Name}> " + $"(client {configuratorClient}).");
                    continue;
                }

                var factoryKey = (configuratorClient, configuratorName);

                if (configuratorFactories.TryAdd(factoryKey, configurator.ConfiguratorSourceFactory))
                {
                    owners[factoryKey] = configurator.ConfuguratorType;
                }
                else
                {
                    AddError($"Configurator '{configuratorName}' for {configuratorClient} is registered twice: {owners[factoryKey].Name} and {configuratorTypeName}.");
                }
            }

            if (errors is null)
            {
                return (true, configuratorFactories, null);
            }
            else
            {
                return (false, null, errors);
            }
        }

        private static (bool success, Dictionary<ClientType, ConstraintRegistration>? constraints, List<string>? errorMessages) ValidateConstraints
            (List<ConstraintRegistration> constraintRegistrations)
        {
            List<string>? errors = null;

            Dictionary<ClientType, ConstraintRegistration> constraints = [];

            Dictionary<Type, ClientType> owners = [];

            void AddError(string errorMessage)
            {
                errors ??= [];

                errors.Add(errorMessage);
            }

            foreach (var constraint in constraintRegistrations)
            {
                var client = constraint.ConstraintClient;

                var type = constraint.ConstraintInterfaceType;

                if (!type.IsInterface)
                {
                    AddError($"Constraint for {client}: {type.Name} is not an interface.");
                    continue;
                }

                if (type == typeof(IBuilderConfigurator))
                {
                    AddError($"Constraint for {client} is too general: {nameof(IBuilderConfigurator)} matches any configurator.");
                    continue;
                }

                if (!constraints.TryAdd(client, constraint))
                {
                    AddError($"Constraint for {client} is registered more than once " + $"({constraints[client].ConstraintInterfaceType.Name}, {type.Name}).");
                    continue;
                }

                if (!owners.TryAdd(type, client))
                {
                    AddError($"{type.Name} is registered as a constraint for both {owners[type]} and {client}.");
                }
            }

            if (errors is null)
            {
                return (true, constraints, null);
            }
            else
            {
                return (false, null, errors);
            }
        }

    }
}
