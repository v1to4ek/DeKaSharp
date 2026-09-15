using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DeKaSharp
{
    internal class ChannelRouter : IAsyncCleanable
    {
        private Channel<InputMessage>? _inputChannel;

        private readonly ConcurrentDictionary<string, Channel<OutputMessage>> _outputChannels;

        public ChannelRouter()
        {
            _inputChannel = null;

            _outputChannels = [];
        }

        public void RegisterChannel(string id)
        {
            var added = _outputChannels.TryAdd(id, Channel.CreateUnbounded<OutputMessage>());

            if (added) Logger.Log($"Добавлен канал с id: {id}");
            else throw new Exception($"Ошибка добавления канала с id: {id}");
        }

        public bool PublishItem(InputMessage message)
        {
            CheckChannels(message.Id);

            if (_inputChannel!.Writer.TryWrite(message))
            {
                return true;
            }
            else return false;
        }

        public ValueTask PublishItemAsync(InputMessage message, CancellationToken ct)
        {
            CheckChannels(message.Id);

            return _inputChannel!.Writer.WriteAsync(message, ct);
        }

        private void CheckChannels(string id)
        {
            if (_inputChannel == null)
            {
                throw new InvalidOperationException("Входной канал не инициализирован: сервис не запущен.");
            }

            if (!_outputChannels.ContainsKey(id))
            {
                throw new InvalidOperationException($"Не найден канал с id: {id}");
            }
        }

        public (bool found, Channel<OutputMessage>? channel) GetChannelById(string id)
        {
            if (_outputChannels.TryGetValue(id, out var channel))
            {
                return (true, channel);
            }
            else
            {  
                Logger.Log($"Не найден канал с id: {id}");

                return (false, null);
            }
        }

        public Task RunAsync(CancellationToken ct)
        {
            Logger.Log("Запуск сервиса роутера входных каналов");

            if (_inputChannel == null)
            {
                _inputChannel = Channel.CreateUnbounded<InputMessage>();

                Logger.Log("Создан входной канал");
            }

            var routerTask = Task.Run(async () =>
            {
                await foreach (var item in _inputChannel.Reader.ReadAllAsync(ct))
                {
                    if (_outputChannels.TryGetValue(item.Id, out var outputChannel))
                    {
                        await outputChannel.Writer.WriteAsync(new OutputMessage(item.MessageKey, item.MessageValue), ct);
                    }
                    else
                    {
                        Logger.Log($"Не найден канал для id: {item.Id}");

                    }
                }
            }, ct);

            Logger.Log("Запущен сервис роутера входных каналов");

            return routerTask;
        }

        private void Clear()
        {
            Logger.Log("Очистка каналов входного роутера");

            _inputChannel?.Writer.Complete();

            foreach (var channel in _outputChannels.Values)
            {
                channel.Writer.Complete();
            }

            _outputChannels.Clear();

            Logger.Log("Каналы входного роутера очищены");
        }

        public Task CleanAsync() => Task.Run(Clear);
    }
}
