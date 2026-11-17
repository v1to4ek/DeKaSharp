namespace DeKaSharp.Routing
{
    //заменить на структуру
    internal record class Message<K, V>
    {
        public required K MessageKey { get; init; }

        public required V MessageValue { get; init; }
    }
}
