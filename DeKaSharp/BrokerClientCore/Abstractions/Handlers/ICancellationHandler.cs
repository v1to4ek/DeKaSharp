namespace DeKaSharp.BrokerClientCore.Abstractions.Handlers
{
    internal interface ICancellationHandler
    {
        public void HandleCancellation()
        {
            //обработка отмены задачи, если нужно
        }
    }
}
