namespace DeKaSharp.Routing.Abstractions
{
    internal interface IUnmanagedReader : IMessageChannel
    {
        public (bool, IntPtr, IntPtr) Read();
    }
}
