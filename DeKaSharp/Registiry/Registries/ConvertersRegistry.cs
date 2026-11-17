using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Registries
{
    internal class ConvertersRegistry : IInitializableRegistry, IProviderSupplier<IConvertersProvider>
    {
        private class ConvertersProvider : IConvertersProvider
        {
            private readonly FrozenDictionary<(DataType dataType, ChannelType channelType), Func<IPointerConverter>> _converterFactories;

            public ConvertersProvider(FrozenDictionary<(DataType dataType, ChannelType channelType), Func<IPointerConverter>> converterFactories)
                => _converterFactories = converterFactories;

            public FrozenDictionary<(DataType dataType, ChannelType channelType), Func<IPointerConverter>> GetConverters() => _converterFactories;
        }

        public string RegistryName => nameof(ConvertersRegistry);

        private readonly Dictionary<(DataType dataType, ChannelType channelType), Func<IPointerConverter>> _tempConverterFactories;

        private IConvertersProvider? _convertersProvider;

        private bool _isInitialized;

        public ConvertersRegistry()
        {
            _tempConverterFactories = [];

            _convertersProvider = null;

            _isInitialized = false;
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            if (_tempConverterFactories.Count == 0) throw new InvalidOperationException();

            var frozenConverterFactories = _tempConverterFactories.ToFrozenDictionary();

            _convertersProvider = new ConvertersProvider(frozenConverterFactories);

            _isInitialized = true;
        }

        //нужна будет реализация со статическими абстрактными свойствами, чтобы избавиться от аллокаций, сделать такой интерфейс и добавить его констрейн
        public void RegisterConverter<TConverter>() where TConverter : IPointerConverter, new()
        {
            if (_isInitialized) throw new InvalidOperationException();

            var converter = new TConverter();

            var key = (converter.DataType, converter.ChannelType);

            Func<IPointerConverter> value = () => new TConverter();

            _tempConverterFactories.Add(key, value);
        }

        public IConvertersProvider GetProvider()
        {
            if (!_isInitialized) throw new InvalidOperationException();

            return _convertersProvider!;
        }
    }
}
