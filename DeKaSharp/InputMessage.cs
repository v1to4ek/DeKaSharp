namespace DeKaSharp
{
    internal record class InputMessage 
    {
        public string Id { get; private set; }

        public string MessageKey { get; private set; }

        public string MessageValue { get; private set; }

        public InputMessage(string  id, string messageKey, string messageValue)
        {
            Id = id;

            MessageKey = messageKey;

            MessageValue = messageValue;
        }
    }
}
