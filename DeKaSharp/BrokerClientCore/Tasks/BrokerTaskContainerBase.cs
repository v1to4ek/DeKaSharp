using DeKaSharp.BrokerClientCore.Abstractions.Configurators.ParameterConfigurators;
using DeKaSharp.BrokerClientCore.Abstractions.Handlers;
using DeKaSharp.BrokerClientCore.Abstractions.TaskContainers;
using DeKaSharp.BrokerClientCore.Enums;
using DeKaSharp.BrokerClientCore.Parameters.Enums;

namespace DeKaSharp.BrokerClientCore.Tasks
{
    internal abstract class BrokerTaskContainerBase : IBrokerTaskContainer, IConfigurableTaskContainer
    {
        public ClientType ClientType { get; }

        public Guid Id { get; }

        protected readonly IParametersSource _parametersProvider;

        private readonly List<ICancellationHandler> _cancellationHandlers;

        private readonly List<IErrorHandler> _errorHandlers;

        public BrokerTaskContainerBase(Guid id, IParametersSource parameters, ClientType clientType)
        {
            Id = id;

            _parametersProvider = parameters;

            ClientType = clientType;

            _cancellationHandlers = [];

            _errorHandlers = [];
        }

        public Func<Task> GetTask(CancellationToken ct)
        {
            var cancellationExceptionParameter = _parametersProvider.GetParameter<bool>(BaseClientParameter.ThrowExOnCancellation);

            var errorExceptionParameter = _parametersProvider.GetParameter<bool>(BaseClientParameter.ThrowExOnError);

            return () => Task.Run(
                async () =>
                {
                    try
                    {
                        await ExecuteAsync(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        if (_cancellationHandlers.Count != 0)
                        {
                            foreach (var handler in _cancellationHandlers)
                            {
                                handler.HandleCancellation();
                            }
                        }

                        if (cancellationExceptionParameter)
                        {
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_errorHandlers.Count != 0)
                        {
                            foreach (var handler in _errorHandlers)
                            {
                                handler.HandleError(ex);
                            }
                        }

                        if (errorExceptionParameter)
                        {
                            throw;
                        }
                    }
                },
                ct);
        }

        protected abstract Task ExecuteAsync(CancellationToken ct);

        public void AddCancellationHandler(ICancellationHandler cancellationHandler) => _cancellationHandlers.Add(cancellationHandler);

        public void AddErrorHandler(IErrorHandler errorHandler) => _errorHandlers.Add(errorHandler);

        public abstract void AddResponseHandler(IResponseHandler handler);

        public Task CleanAsync() => CleanContainerAsyncBase();

        public abstract Task CleanContainerAsyncBase();
    }
}
