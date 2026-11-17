using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Handlers
{
    //интерфейс для обработчиков ответов
    internal interface IResponseHandler
    {
        public ClientType ClientType { get; }

        public ResponseHandlerType ResponseHandlerType { get; }

        public void Clear()
        {
            //очистка ресурсов, если нужно
        }
    }
}
