using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeKaSharp
{
    public static class Adapter
    {
        private static readonly BrokerClientService _brokerHandler;

        private static readonly Lock _locker = new Lock();

        private static bool _startIsBlocked = false;

        static Adapter()
        {
            _brokerHandler = new BrokerClientService();

            _startIsBlocked = false;
        }

        [UnmanagedCallersOnly(EntryPoint = "BuildProducer", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr BuildProducer(IntPtr serverId, IntPtr topic)
        {
            try
            {
                Span<IntPtr> pointerSpan = [serverId, topic];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель");

                var serverHost = Marshal.PtrToStringAnsi(serverId);

                var topicName = Marshal.PtrToStringAnsi(topic);

                var producerId = _brokerHandler.AddProducer(serverHost!, topicName!);

                var ptr = Marshal.StringToCoTaskMemUni(producerId);

                return ptr;
            }
            catch(Exception ex)
            {
                //сделать лог ошибки
                return IntPtr.Zero;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "BuildConsumer", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr BuildConsumer(IntPtr serverId, IntPtr groupId, IntPtr topic)
        {
            try
            {
                Span<IntPtr> pointerSpan = [serverId, groupId, topic];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель");

                var serverHost = Marshal.PtrToStringAnsi(serverId);

                var groupName = Marshal.PtrToStringAnsi(groupId);

                var topicName = Marshal.PtrToStringAnsi(topic);

                var consumerId = _brokerHandler.AddConsumer(serverHost!, groupName!, topicName!);

                var ptr = Marshal.StringToCoTaskMemUni(consumerId);

                return ptr;
            }
            catch(Exception ex)
            {
                //сделать лог ошибки
                return IntPtr.Zero;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "StartService", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Start() 
        {
            lock (_locker)
            {
                if(_startIsBlocked) return;

                _startIsBlocked = true;
            }
            
            try
            {
                _ = _brokerHandler.StartServiceAsync();
            }
            catch(Exception ex)
            {

            }
        }

        [UnmanagedCallersOnly(EntryPoint = "StopService", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Stop()
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        [UnmanagedCallersOnly(EntryPoint = "Produce", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Produce()
        { 
        
        }

        [UnmanagedCallersOnly(EntryPoint = "Consume", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Consume()
        { 
        
        }
    }
}
