using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;

namespace DeKaSharp.Routing.Routers
{
    internal class DefaultRouter : DataRouterBase, IRouterTypeDescriptor
    {
        public static RouterType RouterType => RouterType.WithoutCache;

        public DefaultRouter(IRegistryProvider registryProvider) : base(registryProvider) {}

        public override IUnmanagedReader GetUnmanagedReader(Guid id)
        {
            if (Started == false) throw new InvalidOperationException();

            if (!GetAllChannels.TryGetValue(id, out var channel)) throw new KeyNotFoundException();

            if (channel.ChannelType != ChannelType.Output) throw new ArgumentException();

            var reader = channel as IUnmanagedReader ?? throw new InvalidCastException();

            return reader;

        }

        public override IUnmanagedSender GetUnmanagedSender(Guid id)
        {
            if (Started == false) throw new InvalidOperationException();

            if (!GetAllChannels.TryGetValue(id, out var channel)) throw new KeyNotFoundException();

            if (channel.ChannelType != ChannelType.Input) throw new ArgumentException();

            var sender = channel as IUnmanagedSender ?? throw new InvalidCastException();

            return sender;
        }
    }
}
