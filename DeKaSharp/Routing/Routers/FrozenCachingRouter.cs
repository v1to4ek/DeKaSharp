using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using System.Collections.Frozen;

namespace DeKaSharp.Routing.Routers
{
    internal class FrozenCachingRouter : DataRouterBase, IRouterTypeDescriptor
    {
        public static RouterType RouterType => RouterType.WithFrozenCache;

        private readonly Dictionary<Guid, IUnmanagedReader> _tempReaders;

        private readonly Dictionary<Guid, IUnmanagedSender> _tempSenders;

        private FrozenDictionary<Guid, IUnmanagedReader>? _unmanagedReadersFrozenCache;

        private FrozenDictionary<Guid, IUnmanagedSender>? _unmanagedSendersFrozenCache;

        public FrozenCachingRouter(IRegistryProvider registryProvider) : base(registryProvider)
        {
            _tempReaders = [];

            _tempSenders = [];
        }

        public override IUnmanagedReader GetUnmanagedReader(Guid id)
        {
            var cache = _unmanagedReadersFrozenCache ?? throw new InvalidOperationException();

            if (cache.TryGetValue(id, out var cachedReader))
            {
                return cachedReader;
            }
            else
            {
                //как минимум залоггировать промах кеша

                var channel = GetUnmanagedChannel<IUnmanagedReader>(id, ChannelType.Output);

                return channel;
            }
        }

        public override IUnmanagedSender GetUnmanagedSender(Guid id)
        {
            var cache = _unmanagedSendersFrozenCache ?? throw new InvalidOperationException();

            if (cache.TryGetValue(id, out var cachedSender))
            {
                return cachedSender;
            }
            else
            {
                //как минимум залоггировать промах кеша

                var channel = GetUnmanagedChannel<IUnmanagedSender>(id, ChannelType.Input);

                return channel;
            }
        }

        private TChannel GetUnmanagedChannel<TChannel>(Guid id, ChannelType expectedType) where TChannel : IMessageChannel
        {
            var result = GetAllChannels.TryGetValue(id, out var channel);

            if (result == false || channel == null) throw new KeyNotFoundException();

            if (channel.ChannelType != expectedType) throw new ArgumentException();

            if (channel is TChannel expectedChannel)
            {
                return expectedChannel;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        protected override void OnStart()
        {
            foreach (var keyValuePair in GetAllChannels)
            {
                if (keyValuePair.Value.ChannelType == ChannelType.Input)
                {
                    if (keyValuePair.Value is IUnmanagedSender sender)
                    {
                        _tempSenders.Add(keyValuePair.Key, sender);
                    }
                    else
                    {
                        throw new InvalidCastException();
                    }
                }
                if (keyValuePair.Value.ChannelType == ChannelType.Output)
                {
                    if(keyValuePair.Value is IUnmanagedReader reader)
                    {
                        _tempReaders.Add(keyValuePair.Key, reader);
                    }
                    else
                    {
                        throw new InvalidCastException();
                    }
                }
            }

            _unmanagedReadersFrozenCache = _tempReaders.ToFrozenDictionary();

            _unmanagedSendersFrozenCache = _tempSenders.ToFrozenDictionary();
        }

        protected override void OnStop()
        {
            _tempReaders.Clear();

            _tempSenders.Clear();

            _unmanagedReadersFrozenCache = null;

            _unmanagedSendersFrozenCache = null;

        }
    }
}
