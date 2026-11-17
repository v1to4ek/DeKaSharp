using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;
using System.Threading.Channels;

namespace DeKaSharp.Routing.Channels
{
    internal class InputChannel<TKey, TValue> : IUnmanagedSender, IManagedReader<TKey, TValue>
    {
        public Guid Id { get; }

        public DataType KeyType { get; }

        public DataType ValueType { get; }

        public ChannelType ChannelType => ChannelType.Input;

        private readonly Channel<Message<TKey, TValue>> _channel;

        private readonly IInputConverter<TKey> _keyConverter;

        private readonly IInputConverter<TValue> _valueConverter;

        public InputChannel(Guid id,
            DataType keyType,
            DataType valueType,
            Channel<Message<TKey, TValue>> channel,
            IInputConverterHandler handler)
        {
            Id = id;

            KeyType = keyType;

            ValueType = valueType;

            _channel = channel;

            _keyConverter = handler.GetInputConverter<TKey>();

            _valueConverter = handler.GetInputConverter<TValue>();
        }

        //минимальная реализация, возможно стоит заменить возвращаемый тип на bool
        public void Send(nint dataKey, nint dataValue)
        {
            var key = _keyConverter.Convert(dataKey);

            var value = _valueConverter.Convert(dataValue);

            //тип Message можно попробовать заменить на структуру в будущем, чтобы избавиться от аллокаций
            var message = new Message<TKey, TValue>
            {
                MessageKey = key,
                MessageValue = value
            };

            if (!_channel.Writer.TryWrite(message)) throw new Exception();
        }

        //может полезно было бы реализовать механизм и с ReadAsync методом
        public IAsyncEnumerable<Message<TKey, TValue>> ReadAsync(CancellationToken ct)
        {
            return _channel.Reader.ReadAllAsync(ct);
        }

        public void Clean()
        {
            throw new NotImplementedException();
        }
    }
}
