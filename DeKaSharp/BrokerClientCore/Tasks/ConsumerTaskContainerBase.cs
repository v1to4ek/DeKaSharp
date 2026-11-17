using Dekaf.Consumer;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Handlers;
using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Tasks
{
    internal class ConsumerTaskContainerBase<TKey,TValue> : BrokerTaskContainerBase
    {
        protected readonly IKafkaConsumer<TKey, TValue> _consumer;

        protected readonly List<IConsumerResponseHandler> _responseHandlers;

        protected readonly List<IConsumerResponseHandlerAsync> _asyncResponseHandlers;

        public ConsumerTaskContainerBase(Guid id,
            IParametersSource parameters,
            IKafkaConsumer<TKey, TValue> consumer)
            : base(id, parameters, ClientType.Consumer)
        {
            _consumer = consumer;

            _responseHandlers = [];

            _asyncResponseHandlers = [];
        }

        //блок try 

        //в будущем стоит пересмотреть логику работы хэндлеров ответов, возможно стоит создать отдельный поток для их работы и передавать туда данные из цикла консьюмера/продьюсера
        //https://share.google/aimode/YJj5Ul6YSRAtHHNQV
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await foreach (var message in _consumer.ConsumeAsync(ct))
            {
                var messageKey = message.Key;

                var messageValue = message.Value;

                var context = new ConsumerResponseContext<TKey, TValue>
                {
                    MessageKey = messageKey,

                    MessageValue = messageValue,

                    Result = message 
                };

                await ExecuteHandlersAsync(context);
            }
        }

        //последовательное ожидание задач
        protected virtual async Task ExecuteHandlersAsync(ConsumerResponseContext<TKey, TValue> context)
        {
            if (_responseHandlers.Count != 0)
            {
                foreach (var handler in _responseHandlers)
                {
                    handler.HandleResponse(context);
                }
            }

            if (_asyncResponseHandlers.Count != 0)
            {
                foreach(var handler in _asyncResponseHandlers)
                {
                    await handler.HandleResponseAsync(context);
                }
            }
        }

        public override void AddResponseHandler(IResponseHandler handler)
        {
            if (handler is IConsumerResponseHandler responseHandler)
            {
                _responseHandlers.Add(responseHandler);
            }
            else if(handler is IConsumerResponseHandlerAsync responseHandlerAsync)
            {
                _asyncResponseHandlers.Add(responseHandlerAsync);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public override Task CleanContainerAsyncBase()
        {
            return CleanAsync();
        }

        protected virtual Task CleanContainerAsync() => Task.CompletedTask;
    }
}
