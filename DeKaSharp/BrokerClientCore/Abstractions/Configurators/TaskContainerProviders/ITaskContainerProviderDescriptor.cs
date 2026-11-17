using DeKaSharp.Registiry.Abstractions;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerProviders
{
    internal interface ITaskContainerProviderInstanceDescriptor<TSelf> where TSelf : ITaskContainerProviderSource<ITaskContainerProvider>
    {
        public static abstract string Name { get; }

        public static virtual IReadOnlyCollection<Type>? RequiredProviderRegistries => null;

        public static abstract TSelf Create(IDependencyProvider provider);
    }
}
