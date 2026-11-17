using DeKaSharp.BrokerClientCore.Abstractions.Handlers;
using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.TaskContainers
{
    internal interface IConfigurableTaskContainer
    {
        public ClientType ClientType { get; }

        public void AddCancellationHandler(ICancellationHandler cancellationHandler);

        public void AddErrorHandler(IErrorHandler errorHandler);

        public void AddResponseHandler(IResponseHandler handler);
    }
}
