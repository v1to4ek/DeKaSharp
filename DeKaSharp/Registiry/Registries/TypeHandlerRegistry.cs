using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Types;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Registries
{
    internal class TypeHandlerRegistry : IInitializableRegistry, IProviderSupplier<ITypeDescriptorProvider>
    {
        private class TypeHandler : ITypeDescriptorProvider
        {
            private readonly FrozenDictionary<Type, DataType> _typeDescriptors;

            public TypeHandler(FrozenDictionary<Type, DataType> typeDescriptors) => _typeDescriptors = typeDescriptors;

            public FrozenDictionary<Type, DataType> GetAllDataTypes() => _typeDescriptors;

            public DataType GetTypeDescriptor<TType>()
            {
                var typeItem = typeof(TType);

                if (_typeDescriptors.Any(item => item.Key == typeItem))
                {
                    return _typeDescriptors[typeItem];
                }
                else
                {
                    return DataType.Unregistered;
                }
            }
        }

        public string RegistryName => nameof(TypeHandlerRegistry);

        private readonly Dictionary<Type, DataType> _tempTypeDescriptors;

        private ITypeDescriptorProvider? _provider;

        private bool _isInitialized;

        public TypeHandlerRegistry() => _tempTypeDescriptors = [];

        public void RegisterTypeDescriptor(Type type, DataType typeDescriptor)
        {
            if (_isInitialized) throw new InvalidOperationException();

            _tempTypeDescriptors.Add(type, typeDescriptor);
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            if (_tempTypeDescriptors.Count == 0) throw new InvalidOperationException();

            var frozenTypeDescriptors = _tempTypeDescriptors.ToFrozenDictionary();

            _provider = new TypeHandler(frozenTypeDescriptors);

            _tempTypeDescriptors.Clear();

            _isInitialized = true;
        }

        public ITypeDescriptorProvider GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _provider!;
        }
    }
}
