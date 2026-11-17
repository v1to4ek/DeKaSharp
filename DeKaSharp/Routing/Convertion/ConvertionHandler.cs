using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Registiry.Registries;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;

namespace DeKaSharp.Routing.Convertion
{
    internal class ConvertionHandler : IInputConverterHandler, IOutputConverterHandler
    {
        private readonly IConvertersProvider _convertersProvider;

        private readonly ITypeDescriptorProvider _descriptorProvider;

        public ConvertionHandler(IDependencyProvider registryProvider)
        {
            var convertersProvider = registryProvider.GetIndependentRegistryProvider<ConvertersRegistry,IConvertersProvider>();

            var descriptorProvider = registryProvider.GetIndependentRegistryProvider<TypeHandlerRegistry,ITypeDescriptorProvider>();

            CheckConverters(convertersProvider, descriptorProvider);

            _convertersProvider = convertersProvider;

            _descriptorProvider = descriptorProvider;
        }

        //проверка на соответствие, чтобы для всех зарегистрированных типов был зарегистрирован конвертер
        private static void CheckConverters(IConvertersProvider convertersProvider, ITypeDescriptorProvider typeDescriptorProvider)
        {
            var types = typeDescriptorProvider.GetAllDataTypes();

            var converters = convertersProvider.GetConverters();

            var inputConverterTypes = converters
                .Where(c => c.Key.channelType == ChannelType.Input)
                .Select(c => c.Key.dataType)
                .ToHashSet();

            var outputConverterTypes = converters
                .Where(c => c.Key.channelType == ChannelType.Output)
                .Select(c => c.Key.dataType)
                .ToHashSet();

            foreach (var dataType in types.Values)
            {
                var inputRes = inputConverterTypes.Contains(dataType);

                var outputRes = outputConverterTypes.Contains(dataType);

                if (!inputRes || !outputRes) throw new InvalidOperationException();
            }
        }

        public IOutputConverter<TData> GetOutputConverter<TData>()
        {
            var dataType = GetDataType<TData>();

            var converterKeyValue = _convertersProvider
                .GetConverters()
                .Where(item => item.Key.channelType == ChannelType.Output)
                .FirstOrDefault(item => item.Key.dataType == dataType);

            var converterFactory = converterKeyValue.Value ?? throw new ArgumentException();

            var converter = converterFactory() as IOutputConverter<TData> ?? throw new ArgumentException();

            return converter;
        }

        public IInputConverter<TData> GetInputConverter<TData>()
        {
            var dataType = GetDataType<TData>();

            var converterKeyValue = _convertersProvider
                .GetConverters()
                .Where(item => item.Key.channelType == ChannelType.Input)
                .FirstOrDefault(item => item.Key.dataType == dataType);

            var converterFactory = converterKeyValue.Value ?? throw new ArgumentException();

            var converter = converterFactory() as IInputConverter<TData> ?? throw new ArgumentException();

            return converter;
        }

        private DataType GetDataType<TData>()
        {
            var dataType = _descriptorProvider.GetTypeDescriptor<TData>();

            if (dataType == DataType.Unregistered) throw new ArgumentException();

            return dataType;
        }
    }
}
