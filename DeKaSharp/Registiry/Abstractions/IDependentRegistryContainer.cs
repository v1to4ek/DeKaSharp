using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IDependentRegistryContainer
    {
        public void Register<TRegistry, TProvider>(TRegistry registryInstance)
            where TRegistry : class, IDependentInitializableRegistry, IProviderSupplier<TProvider>
            where TProvider : IDataProvider;

        public void Register<TRegistry, TProvider>(Action<TRegistry> registryApplicator)
            where TRegistry : class, IDependentInitializableRegistry, IProviderSupplier<TProvider>, new()
            where TProvider : IDataProvider;
    }
}
