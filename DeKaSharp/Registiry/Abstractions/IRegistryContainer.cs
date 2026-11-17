using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IRegistryContainer
    {
        public void Register<TRegistry, TProvider>(TRegistry registryInstance)
            where TRegistry : class, IInitializableRegistry, IProviderSupplier<TProvider>
            where TProvider : IDataProvider;

        public void Register<TRegistry, TProvider>(Action<TRegistry> registryApplicator)
            where TRegistry : class, IInitializableRegistry, IProviderSupplier<TProvider>, new()
            where TProvider : IDataProvider;
    }
}
