using DeKaSharp.Routing.Enums;
using System.Threading.Channels;

namespace DeKaSharp.Routing.Abstractions
{
    internal interface IManagedRouter
    {
        public IManagedSender<TKey, TValue> GetManagedSender<TKey, TValue>(Guid id);

        public IManagedReader<TKey, TValue> GetManagedReader<TKey, TValue>(Guid id);

        public bool RegisterChannel<TKey, TValue>(Guid id, Channel<Message<TKey, TValue>> channel, ChannelType channelType);
    }
}
