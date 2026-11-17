using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IRegistryProvider
    {
        public TProvider GetProvider<TProvider>()
            where TProvider : IDataProvider;
    }
}
