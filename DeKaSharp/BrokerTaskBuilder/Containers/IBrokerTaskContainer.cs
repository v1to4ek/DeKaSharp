namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal interface IBrokerTaskContainer
    {
        public BrokerTaskType Type { get; }

        public string Id { get; }

        public Func<Task> GetTaskAction();

        public void SubscribeToErrorEvent(Action<string>? onErrorCallback);

        public Task CleanAsync();
    }
}
