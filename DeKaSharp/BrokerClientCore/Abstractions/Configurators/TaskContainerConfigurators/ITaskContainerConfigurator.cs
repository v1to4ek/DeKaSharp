using DeKaSharp.BrokerClientCore.Abstractions.TaskContainers;
using DeKaSharp.BrokerClientCore.Enums;

namespace DeKaSharp.BrokerClientCore.Abstractions.Configurators.TaskContainerConfigurators
{
    //разобраться с декораторами
    internal interface ITaskContainerConfigurator
    {
        public ClientType ClientType { get; }

        public void Configure(IConfigurableTaskContainer taskContainer);
    }
}
