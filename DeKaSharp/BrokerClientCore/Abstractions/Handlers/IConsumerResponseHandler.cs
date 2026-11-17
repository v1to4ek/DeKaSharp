using Dekaf.Consumer;

namespace DeKaSharp.BrokerClientCore.Abstractions.Handlers
{
    //можно заменить на структуру для увеличения производительности
    public record class ConsumerResponseContext<TKey, TValue>
    {
        public TKey? MessageKey { get; set; }

        public TValue? MessageValue { get; set; }

        public ConsumeResult<TKey, TValue>? Result { get; set; }
    };

    //возможно стоит добавить асинхронную версию HandleResponse, чтобы не блокировать поток

    internal interface IConsumerResponseHandler : IResponseHandler
    {
        public void HandleResponse<TKey, TValue>(ConsumerResponseContext<TKey, TValue> context);
    }

    internal interface IConsumerResponseHandlerAsync : IResponseHandler
    {
        public ValueTask HandleResponseAsync<TKey, TValue>(ConsumerResponseContext<TKey, TValue> context);
    }
}
