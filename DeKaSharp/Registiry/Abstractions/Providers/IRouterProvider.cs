using DeKaSharp.Routing.Abstractions;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IRouterProvider : IDataProvider
    {
        public IManagedRouter GetForManaged();

        public IUnmanagedRouter GetForUnmanaged();

        public IRouterLifecycle GetRouterLifecycle();
    }
}
