using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using System.Collections.Concurrent;

namespace DeKaSharp.Routing.Routers
{
    internal class DefaultCachingRouter : DataRouterBase, IRouterTypeDescriptor
    {
        public static RouterType RouterType => RouterType.WithDefaultCache;

        private readonly ConcurrentDictionary<Guid, IUnmanagedReader> _readersCache;

        private readonly ConcurrentDictionary<Guid, IUnmanagedSender> _sendersCache;

        public DefaultCachingRouter(IRegistryProvider registryProvider) : base(registryProvider)
        {
            _readersCache = [];

            _sendersCache = [];
        }

        public override IUnmanagedReader GetUnmanagedReader(Guid id)
        {
            if (_readersCache.TryGetValue(id, out var reader))
            {
                return reader;
            }
            else
            {
                try
                {
                    if (Started == false) throw new InvalidOperationException();

                    var uncached = GetAllChannels[id];

                    if (uncached.ChannelType != ChannelType.Output) throw new ArgumentException();

                    var tempReader = uncached as IUnmanagedReader ?? throw new InvalidCastException();

                    _readersCache.TryAdd(id, tempReader);

                    return tempReader;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public override IUnmanagedSender GetUnmanagedSender(Guid id)
        {
            if (_sendersCache.TryGetValue(id, out var sender))
            {
                return sender;
            }
            else
            {
                try
                {
                    if (Started == false) throw new InvalidOperationException();

                    var uncached = GetAllChannels[id];

                    if (uncached.ChannelType != ChannelType.Input) throw new ArgumentException();

                    var tempSender = uncached as IUnmanagedSender ?? throw new InvalidCastException();

                    _sendersCache.TryAdd(id, tempSender);

                    return tempSender;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        protected override void OnStop()
        {
            _readersCache.Clear();

            _sendersCache.Clear();
        }
    }
}
