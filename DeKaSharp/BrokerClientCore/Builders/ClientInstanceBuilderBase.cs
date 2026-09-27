using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerClientCore.Builders
{
    internal enum ClientParameter
    {
        Topic
    }

    internal interface IKafkaClient<TInnerClient>
    {
        public TInnerClient Client { get; }

        public Dictionary<ClientParameter, string>? Params { get; }
    }

    internal abstract class ClientInstanceBuilderBase<TKafkaBuilder, TResultClient, TKey, TValue>
        where TKafkaBuilder : class
    {
        public bool ShouldBeDeleted { get; private set; }

        public Guid InstanceId { get; private set; }

        protected TKafkaBuilder? ClientBuilder;

        protected Dictionary<ClientParameter, string>? Params;

        private Func<TKafkaBuilder>? _clientBuilderFactory;

        protected ClientInstanceBuilderBase(Func<TKafkaBuilder> clientBuilderFactory)
        {
            _clientBuilderFactory = clientBuilderFactory;

            ClientBuilder = clientBuilderFactory();

            Params = [];

            InstanceId = Guid.NewGuid();

            ShouldBeDeleted = false;

            Logger.Log($"Создан строиель клиента c id: {InstanceId}");
        }

        public void Configure(Action<TKafkaBuilder> configurator)
        {
            configurator(ClientBuilder!);

            Logger.Log($"Выполнен шаг конфигурации строителя клиента с id: {InstanceId} через делегат");
        }

        public void Configure(IClientBuilderConfigurator<TKafkaBuilder> configurator)
        {
            configurator.Configure(ClientBuilder!);

            Logger.Log($"Выполнен шаг конфигурации строителя клиента с id: {InstanceId} через функциональный интерфейс");
        }

        public void AddParameter(ClientParameter parameter, string value)
        {
            Params![parameter] = value;

            Logger.Log($"Добавлен параметр для строителя клиента с id: {InstanceId}");
        }

        public TResultClient Build(bool stopBuilder = true)
        {
            var client = BuildClient();

            if (stopBuilder)
            {
                StopBuilder();
            }
            else
            {
                ResetBuilder();
            }

            return client;
        }

        private void StopBuilder()
        {
            ClientBuilder = null;

            Params = null;

            InstanceId = Guid.Empty;

            _clientBuilderFactory = null;

            ShouldBeDeleted = true;
        }

        private void ResetBuilder()
        {
            ClientBuilder = _clientBuilderFactory!();

            Params = [];

            InstanceId = Guid.NewGuid();

            ShouldBeDeleted = false;
        }

        protected abstract TResultClient BuildClient();
    }
}
