using DeKaSharp.Routing;

namespace DeKaSharp.Routing.Abstractions
{
    internal interface IManagedReader<TKey, TValue> : IMessageChannel
    {
        public IAsyncEnumerable<Message<TKey, TValue>> ReadAsync(CancellationToken ct);
    }
}
