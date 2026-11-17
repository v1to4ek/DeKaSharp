using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IDependencyProvider
    {
        public TProvider GetDependentRegistryProvider<TRegistry, TProvider>()
            where TRegistry : class, IDependentInitializableRegistry, IProviderSupplier<TProvider>
            where TProvider : IDataProvider;

        public TProvider GetIndependentRegistryProvider<TRegistry, TProvider>() 
            where TRegistry : class, IInitializableRegistry, IProviderSupplier<TProvider>
            where TProvider : IDataProvider;
    }
}
