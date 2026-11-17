using DeKaSharp.Routing;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;
using System.Threading.Channels;

namespace DeKaSharp.Routing.Channels
{
    internal class OutputChannel<TKey, TValue> : IUnmanagedReader, IManagedSender<TKey, TValue>
    {
        public Guid Id { get; }

        public DataType KeyType { get; }

        public DataType ValueType { get; }

        public ChannelType ChannelType => ChannelType.Output;

        private readonly Channel<Message<TKey, TValue>> _channel;

        private readonly IOutputConverter<TKey> _keyConverter;

        private readonly IOutputConverter<TValue> _valueConverter;

        public OutputChannel(Guid id,
            DataType keyType,
            DataType valueType,
            Channel<Message<TKey, TValue>> channel,
            IOutputConverterHandler handler)
        {
            Id = id;

            KeyType = keyType;

            ValueType = valueType;

            _channel = channel;

            _keyConverter = handler.GetOutputConverter<TKey>();

            _valueConverter = handler.GetOutputConverter<TValue>();
        }

        //минимальная реализация
        public (bool, nint, nint) Read()
        {
            if (!_channel.Reader.TryRead(out var item)) throw new Exception();

            var key = _keyConverter.Convert(item.MessageKey);

            var value = _valueConverter.Convert(item.MessageValue);

            return (true, key, value);
        }

        public ValueTask SendAsync(TKey key, TValue value, CancellationToken ct)
        {
            var message = new Message<TKey, TValue>
            {
                MessageKey = key,
                MessageValue = value
            };

            return _channel.Writer.WriteAsync(message, ct);
        }

        public void Clean()
        {
            throw new NotImplementedException();
        }
    }
}
