using DeKaSharp.BrokerClientCore.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerClientCore
{
    internal interface IClientReciver
    {
        public void Recive<TClient>(IKafkaClient<TClient> client);
    }

    internal class BrokerClientTaskBuilder
    {

    }
}
