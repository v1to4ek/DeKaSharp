using Dekaf.Producer;

namespace DeKaSharp.BrokerClientCore.Abstractions.Handlers
{
    //можно заменить на структуру для увеличения производительности
    public record struct ProducerResponseContext
    {
        public RecordMetadata? RecordMetadata { get; set; }
    }

    //обработчик ответа прикрепляется к контейнеру через метод AddResponseHandler, используя ITaskContainerConfigurator
    //класс обработчика может содержать стартегии возврата данных логгирование, коллбеки и много чего другого интереснрго

    //вероятно стоит добавить асинхронную версию HandleResponse, чтобы не блокировать поток
    internal interface IProducerResponseHandler : IResponseHandler
    {
        public void HandleResponse(ProducerResponseContext context);
    }

    internal interface IProducerResponseHandlerAsync : IResponseHandler
    {
        public ValueTask HandleResponseAsync(ProducerResponseContext context);
    }

}
