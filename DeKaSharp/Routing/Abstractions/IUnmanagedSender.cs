namespace DeKaSharp.Routing.Abstractions
{
    internal interface IUnmanagedSender : IMessageChannel
    {
        public void Send(IntPtr dataKey, IntPtr dataValue);
    }
}
