namespace DeKaSharp.Routing.Abstractions
{
    internal interface IUnmanagedRouter
    {
        public IUnmanagedSender GetUnmanagedSender(Guid id);

        public IUnmanagedReader GetUnmanagedReader(Guid id);
    }
}
