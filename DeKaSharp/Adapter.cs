using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeKaSharp
{
    public static class Adapter
    {
        private static readonly BrokerClientService _brokerHandler;

        private static readonly Lock _strartLocker = new();

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
            lock (_strartLocker)
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
                _brokerHandler.StopService();
            }
            catch (Exception ex)
            {

            }
        }


        [UnmanagedCallersOnly(EntryPoint = "Produce", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Produce(IntPtr producerId, IntPtr messageKey, IntPtr messageValue)
        {
            try
            {
                Span<IntPtr> pointerSpan = [producerId, messageKey, messageValue];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель");

                var id = Marshal.PtrToStringAnsi(producerId);

                var key = Marshal.PtrToStringAnsi(messageKey);

                var value = Marshal.PtrToStringAnsi(messageValue);

                var inputMethod = _brokerHandler.InputData();

                var message = new InputMessage(id!, key!, value!);

                inputMethod(message);
            }
            catch(Exception)
            {

            }
        }


        [UnmanagedCallersOnly(EntryPoint = "Consume", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Consume()
        { 
        
        }
    }
}
