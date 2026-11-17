using DeKaSharp.Registiry.Abstractions.Providers;

namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IProviderSupplier<TProvider> where TProvider : IDataProvider
    {
        TProvider GetProvider();
    }
}
