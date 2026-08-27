using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DeKaSharp
{
    internal class ProducingRouter 
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

        public Task Start()
        {
            return Task.Run(async () =>
            {
                await foreach (var item in _inputChannel.Reader.ReadAllAsync())
                {
                    if (_outputChannels.TryGetValue(item.Id, out var outputChannel))
                    {
                        await outputChannel.Writer.WriteAsync(new OutputMessage(item.MessageKey, item.MessageValue));
                    }
                    else
                    {
                        Console.WriteLine("Канала нет");
                    }
                }
            });
        }

    }
}
