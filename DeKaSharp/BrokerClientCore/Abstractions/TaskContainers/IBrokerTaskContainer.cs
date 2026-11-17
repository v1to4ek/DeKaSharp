using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.TaskContainers
{
    internal interface IBrokerTaskContainer
    {
        public ClientType ClientType { get; }

        public Guid Id { get; }

        public Func<Task> GetTask(CancellationToken ct);

        public Task CleanAsync();
    }
}
