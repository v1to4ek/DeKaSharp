using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Routing.Routers;

namespace DeKaSharp.Registiry.Registries
{
    internal class RouterRegistry : IDependentInitializableRegistry, IProviderSupplier<IRouterProvider>
    {
        private class RouterProvider : IRouterProvider
        {
            private readonly DataRouterBase _router;

            public RouterProvider(DataRouterBase router)
            {
                _router = router;
            }

            public IManagedRouter GetForManaged() => _router;

            public IUnmanagedRouter GetForUnmanaged() => _router;

            public IRouterLifecycle GetRouterLifecycle() => _router;
        }

        public string RegistryName => nameof(RouterRegistry);

        private readonly Dictionary<RouterType, Func<IDependencyProvider, DataRouterBase>> _routerFactories;

        private Func<IDependencyProvider, DataRouterBase>? _selectedFactory;

        private IRouterProvider? _routerProvider;

        private bool _isInitialized;

        public RouterRegistry()
        {
            _isInitialized = false;

            _routerFactories = [];

            _selectedFactory = null;

            _routerProvider = null;
        }

        //внутрь коллбэка передаём что должно быть инициализировано провайдером в этом классе после инициализации всех реестров
        public void Initialize(ProviderSupplierCallback supplier)
        {
            if (_isInitialized) return;

            if (_routerFactories.Count == 0) throw new InvalidOperationException();

            _selectedFactory ??= _routerFactories.First().Value;

            supplier(provider =>
            {
                var router = _selectedFactory(provider);

                _routerProvider = new RouterProvider(router);
            });

            _isInitialized = true;
        }

        public void RegisterRouter<TRouter>(Func<IDependencyProvider, TRouter> routerFactory) where TRouter : DataRouterBase, IRouterTypeDescriptor
        {
            if (_isInitialized) throw new InvalidOperationException();

            var type = TRouter.RouterType;

            if (_routerFactories.ContainsKey(type)) throw new ArgumentException();
            
            _routerFactories.Add(type, routerFactory);
        }

        public void SelectRouter(RouterType routerType)
        {
            if (_isInitialized) throw new InvalidOperationException();

            if (_routerFactories.Count == 0) throw new InvalidOperationException();

            var factory = _routerFactories[routerType];

            _selectedFactory = factory;
        }

        public IRouterProvider GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _routerProvider!;
        }
    }
}
