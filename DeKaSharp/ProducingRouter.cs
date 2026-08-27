using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DeKaSharp
{
    internal class ProducingRouter : IDisposable
    {
        private readonly Channel<InputMessage> _inputChannel;

        private readonly ConcurrentDictionary<string, Channel<OutputMessage>> _outputChannels;

        public ProducingRouter()
        {
            _inputChannel = Channel.CreateUnbounded<InputMessage>();

            _outputChannels = [];
        }

        public void RegisterChannel(string id) => _outputChannels.TryAdd(id, Channel.CreateUnbounded<OutputMessage>());

        public void PublishItem(InputMessage message) => _inputChannel.Writer.TryWrite(message);

        public Channel<OutputMessage> GetChannelById(string id) => _outputChannels[id];

        public Task Start(CancellationToken ct)
        {
            return Task.Run(async () =>
            {
                await foreach (var item in _inputChannel.Reader.ReadAllAsync(ct))
                {
                    if (_outputChannels.TryGetValue(item.Id, out var outputChannel))
                    {
                        await outputChannel.Writer.WriteAsync(new OutputMessage(item.MessageKey, item.MessageValue), ct);
                    }
                    else
                    {
                        Console.WriteLine("Канала нет");
                    }
                }
            }, ct);
        }

        public void Dispose()
        {
            _inputChannel.Writer.Complete();

            foreach (var channel in _outputChannels.Values)
            {
                channel.Writer.Complete();
            }

            _outputChannels.Clear();    
        }
    }
}
