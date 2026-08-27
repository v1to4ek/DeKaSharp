using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp
{
    internal class OutputMessage
    {
        public string MessageKey { get; private set; }

        public string MessageValue { get; private set; }

        public OutputMessage(string  messageKey, string messageValue)
        {
            MessageKey = messageKey;
            MessageValue = messageValue;
        }

    }
}
