using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerClientCore.Abstractions.Handlers
{
    internal interface IErrorHandler
    {
        public void HandleError(Exception ex)
        {
            //обработка ошибки, если нужно
        }
    }
}
