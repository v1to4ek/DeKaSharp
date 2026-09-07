using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal interface IBrokerTaskContainer
    {
        public string Id { get; }

        public Func<Task> GetTaskAction();
    }
}
