namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface ITaskContainerProviderSource<out TProvider> where TProvider : ITaskContainerProvider
    {
        public TProvider GetProvider();
    }
}
