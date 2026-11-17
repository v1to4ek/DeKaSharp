using Dekaf.Producer;
using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Handlers;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.BrokerClientCore.Parameters.Enums;
using DeKaSharp.Routing;
using DeKaSharp.Routing.Abstractions;
using DeKaSharp.Routing.Enums;
using System.Threading.Channels;

namespace DeKaSharp.BrokerClientCore.Tasks
{
    internal class ProducerTaskContainerBase<TKey, TValue> : BrokerTaskContainerBase
    {
        protected readonly IKafkaProducer<TKey, TValue> _producer;

        protected readonly IManagedRouter _router;

        protected readonly List<IProducerResponseHandler> responseHandlers;

        protected readonly List<IProducerResponseHandlerAsync> asyncResponseHandlers;

        public ProducerTaskContainerBase(Guid id,
            IParametersSource parameters,
            IKafkaProducer<TKey, TValue> producer,
            IManagedRouter router)
            : base(id, parameters, ClientType.Producer)
        {
            _producer = producer;

            responseHandlers = [];

            asyncResponseHandlers = [];

            var channel = Channel.CreateUnbounded<Message<TKey, TValue>>();

            router.RegisterChannel(id, channel, ChannelType.Input);

            _router = router;
        }

        //блок try 

        //в будущем стоит пересмотреть логику работы хэндлеров ответов, возможно стоит создать отдельный поток для их работы и передавать туда данные из цикла консьюмера/продьюсера
        //https://share.google/aimode/YJj5Ul6YSRAtHHNQV
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var reader = _router.GetManagedReader<TKey, TValue>(Id);

            var topicParameter = _parametersProvider.GetParameter<string>(ProducerTaskContainerBaseParameters.Topic);

            await foreach (var message in reader.ReadAsync(ct))
            {
                var key = message.MessageKey;

                var value = message.MessageValue;

                var data = await _producer.ProduceAsync(topicParameter, key, value, ct);

                var context = new ProducerResponseContext
                {
                    RecordMetadata = data
                };

                await ExecuteHandlersAsync(context);
            }
        }

        //последовательное ожидание задач
        protected virtual async Task ExecuteHandlersAsync(ProducerResponseContext context)
        {
            if (responseHandlers.Count != 0)
            {
                foreach (var handler in responseHandlers)
                {
                    handler.HandleResponse(context);
                }
            }

            if (asyncResponseHandlers.Count != 0)
            {
                foreach (var handler in asyncResponseHandlers)
                {
                    await handler.HandleResponseAsync(context);
                }
            }
        }

        public override void AddResponseHandler(IResponseHandler handler)
        {
            if (handler is IProducerResponseHandler responseHandler)
            {
                responseHandlers.Add(responseHandler);
            }
            else if (handler is IProducerResponseHandlerAsync asyncResponseHandler)
            {
                asyncResponseHandlers.Add(asyncResponseHandler);
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
