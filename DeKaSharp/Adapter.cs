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

                var ptr = Marshal.StringToCoTaskMemAnsi(producerId);

                return ptr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex.Message);

                var ptr = Marshal.StringToCoTaskMemAnsi("err");

                return ptr;
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

                var ptr = Marshal.StringToCoTaskMemAnsi(consumerId);

                return ptr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex.Message);

                return IntPtr.Zero;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "StartService", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr Start() 
        {
            lock (_strartLocker)
            {
                if (_startIsBlocked)
                {
                    Logger.Log("Сервис не запущен из-за блокировки (startIsBlocked) ");

                    return Marshal.StringToCoTaskMemAnsi("err");
                }

                _startIsBlocked = true;

                Logger.Log("Старт сервиса заблокирован в адаптере (startIsBlocked)");
            }
            
            try
            {
                _ = _brokerHandler.StartServiceAsync();

                var ptr = Marshal.StringToCoTaskMemAnsi("ok");

                return ptr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex.Message);

                var ptr = Marshal.StringToCoTaskMemAnsi("err");

                return ptr;
            }
            finally
            {
                _startIsBlocked = false;

                Logger.Log("Блокировка на старт сервиса снята");
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "StopService", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr Stop()
        {
            try
            {
                _brokerHandler.StopService();

                lock (_strartLocker)
                {
                    if (_startIsBlocked) _startIsBlocked = false;

                    Logger.Log("Блокировка на старт сервиса снята");
                }

                return Marshal.StringToCoTaskMemAnsi("ok");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);

                var ptr = Marshal.StringToCoTaskMemAnsi("err");

                return ptr;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "Produce", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr Produce(IntPtr producerId, IntPtr messageKey, IntPtr messageValue)
        {
            try
            {
                Span<IntPtr> pointerSpan = [producerId, messageKey, messageValue];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель");

                var id = Marshal.PtrToStringAnsi(producerId);

                var key = Marshal.PtrToStringAnsi(messageKey);

                var value = Marshal.PtrToStringAnsi(messageValue);

                var message = new InputMessage(id!, key!, value!);

                var success = _brokerHandler.InputData(message);

                if (success)
                {
                    return Marshal.StringToCoTaskMemAnsi("ok");
                }
                else
                {
                    return Marshal.StringToCoTaskMemAnsi("not sent");
                }
            }
            catch(Exception)
            {
                return Marshal.StringToCoTaskMemAnsi("err");
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "Consume", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void Consume()
        { 
        
        }
    }
}
