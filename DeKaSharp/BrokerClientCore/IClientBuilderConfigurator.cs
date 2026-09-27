using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerClientCore
{
    internal interface IClientBuilderConfigurator<TBuilder>
    {
        public void Configure(TBuilder builder);
    }
}
