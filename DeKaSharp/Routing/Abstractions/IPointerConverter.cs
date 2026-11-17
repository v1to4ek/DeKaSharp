using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;

namespace DeKaSharp.Routing.Abstractions
{
    internal interface IPointerConverter
    {
        public ChannelType ChannelType { get; }

        public DataType DataType { get; }
    }
}
