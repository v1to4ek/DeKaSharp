namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IDependentInitializableRegistry : IRegistry
    {
        public IReadOnlyCollection<Type>? RequiredDependentRegistries => null;

        public void Initialize(ProviderSupplierCallback providerSupplierCallback);
    }
}
