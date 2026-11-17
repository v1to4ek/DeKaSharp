using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;

namespace DeKaSharp.Routing.Abstractions
{
    internal interface IMessageChannel
    {
        public DataType KeyType { get; }

        public DataType ValueType { get; }

        public ChannelType ChannelType { get; }

        public Guid Id { get; }

        public void Clean();
    }
}
