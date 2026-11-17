using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IConvertersProvider : IDataProvider
    {
        public FrozenDictionary<(DataType dataType, ChannelType channelType), Func<IPointerConverter>> GetConverters();
    }
}
