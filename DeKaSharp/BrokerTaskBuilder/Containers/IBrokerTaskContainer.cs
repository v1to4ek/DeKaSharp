using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal interface IBrokerTaskContainer
    {
        public BrokerTaskType Type { get; }

        public string Id { get; }

        public Func<Task> GetTaskAction();
    }
}
