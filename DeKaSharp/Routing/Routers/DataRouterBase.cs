using DeKaSharp.Registiry.Abstractions;
using DeKaSharp.Registiry.Abstractions.Providers;
using DeKaSharp.Registiry.Registries;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Channels;
using DeKaSharp.Routing.Convertion;
using DeKaSharp.Routing.Enums;
using DeKaSharp.Types;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DeKaSharp.Routing.Routers
{

    //вынести методы работы с регистрацией и managed каналами в базовый класс, так как их реализация неизменна
    internal abstract class DataRouterBase : IUnmanagedRouter, IManagedRouter, IRouterLifecycle
    {
        public bool Started { get; private set; }

        protected IReadOnlyDictionary<Guid, IMessageChannel> GetAllChannels => _channelsView;

        private readonly IReadOnlyDictionary<Guid, IMessageChannel> _channelsView;

        private readonly ConcurrentDictionary<Guid, IMessageChannel> _allChannels;

        private readonly ConvertionHandler _convertionHandler;

        private readonly ITypeDescriptorProvider _typeDescriptionHandler;

        private readonly Lock _locker;

        protected DataRouterBase(IDependencyProvider registryProvider)
        {
            _convertionHandler = new(registryProvider);

            _typeDescriptionHandler = registryProvider.GetIndependentRegistryProvider<TypeHandlerRegistry, ITypeDescriptorProvider>();

            _allChannels = [];

            _channelsView = _allChannels.AsReadOnly();

            _locker = new();

            Started = false;
        }

        public bool RegisterChannel<TKey, TValue>(Guid id, Channel<Message<TKey, TValue>> channel, ChannelType channelType)
        {
            lock (_locker)
            {
                if (Started == true) throw new InvalidOperationException();

                var keyDataType = _typeDescriptionHandler.GetTypeDescriptor<TKey>();

                var valueDataType = _typeDescriptionHandler.GetTypeDescriptor<TValue>();

                if (keyDataType == DataType.Unregistered || valueDataType == DataType.Unregistered)
                {
                    throw new ArgumentException();
                }

                if (channelType == ChannelType.Input)
                {
                    var channelContainer = new InputChannel<TKey, TValue>(id,
                        keyDataType,
                        valueDataType,
                        channel,
                        _convertionHandler);

                    if (_allChannels.TryAdd(id, channelContainer))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (channelType == ChannelType.Output)
                {
                    var channelContainer = new OutputChannel<TKey, TValue>(id,
                        keyDataType,
                        valueDataType,
                        channel,
                        _convertionHandler);

                    if (_allChannels.TryAdd(id, channelContainer))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }
        }

        public IManagedReader<TKey, TValue> GetManagedReader<TKey, TValue>(Guid id)
        {
            if (Started == false) throw new InvalidOperationException();

            if (!_allChannels.TryGetValue(id, out var channel))
            {
                throw new Exception();
            }

            var keyDataType = _typeDescriptionHandler.GetTypeDescriptor<TKey>();

            var valueDataType = _typeDescriptionHandler.GetTypeDescriptor<TValue>();

            if (keyDataType == DataType.Unregistered || valueDataType == DataType.Unregistered)
            {
                throw new ArgumentException();
            }

            if (channel.ChannelType != ChannelType.Input || channel.KeyType != keyDataType || channel.ValueType != valueDataType)
            {
                throw new Exception();
            }

            var reader = channel as IManagedReader<TKey, TValue> ?? throw new Exception();

            return reader;
        }

        public IManagedSender<TKey, TValue> GetManagedSender<TKey, TValue>(Guid id)
        {
            if (Started == false) throw new InvalidOperationException();

            if (!_allChannels.TryGetValue(id, out var channel))
            {
                throw new Exception();
            }

            var keyDataType = _typeDescriptionHandler.GetTypeDescriptor<TKey>();

            var valueDataType = _typeDescriptionHandler.GetTypeDescriptor<TValue>();

            if (keyDataType == DataType.Unregistered || valueDataType == DataType.Unregistered)
            {
                throw new ArgumentException();
            }

            if (channel.ChannelType != ChannelType.Output || channel.KeyType != keyDataType || channel.ValueType != valueDataType)
            {
                throw new Exception();
            }

            var sender = channel as IManagedSender<TKey, TValue> ?? throw new Exception();

            return sender;
        }

        public abstract IUnmanagedReader GetUnmanagedReader(Guid id);

        public abstract IUnmanagedSender GetUnmanagedSender(Guid id);

        public void Start()
        {
            lock (_locker)
            {
                if (Started == true) throw new InvalidOperationException();

                OnStart();

                Started = true;
            }
        }

        protected virtual void OnStart() { }

        public void Stop()
        {
            lock (_locker)
            {
                if (Started == false) return;

                Started = false;

                OnStop();

                foreach (var channel in _allChannels.Values) channel.Clean();

                _allChannels.Clear();
            }
        }

        protected virtual void OnStop() { }
    }
}
