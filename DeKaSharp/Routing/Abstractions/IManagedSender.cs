namespace DeKaSharp.Routing.Abstractions
{
    internal interface IManagedSender<TKey, TValue> : IMessageChannel
    {
        public ValueTask SendAsync(TKey key, TValue value, CancellationToken ct);
    }
}
