using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DeKaSharp
{
    //1. Добавить out параметры в UnmanagedCallersOnly методы либо создать структуры.
    //2. Добавить маршаллинг и сохранение делегатов, чтобы избежать access violation из-за их удаления gc.
    //3. Добавить возможность остановки отдельного потока исполнения сервиса и остановки всего сервиса целиком в случае ошибки.
    //4. Отписывать коллбеки от событий в классах, где они есть, в случае возникновения исключения в адаптере.
    public static unsafe class Adapter
    {
        private static readonly BrokerService _brokerService;

        private static readonly ConcurrentDictionary<string, Action<string>> _producerCallbacks;

        private static readonly ConcurrentDictionary<string, Action<string, string>> _consumerCallbacks;

        static Adapter()
        {
            _brokerService = new();

            _producerCallbacks = new();

            _consumerCallbacks = new();

            Logger.Log("Вызов конструктора адаптера, создан сервис брокера");
        }

        //Готово
        [UnmanagedCallersOnly(EntryPoint = "RegisterProducer", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr RegisterProducer(IntPtr serverHost, IntPtr topicName)
        {
            try
            {
                Span<IntPtr> pointerSpan = [serverHost, topicName];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель на параметр");

                var server = Marshal.PtrToStringAnsi(serverHost);

                var topic = Marshal.PtrToStringAnsi(topicName);

                var producerId = _brokerService.RegisterProducer(server!, topic!);

                var resultPointer = Marshal.StringToCoTaskMemAnsi(producerId);

                return resultPointer;

            }
            catch(Exception ex)
            {
                Logger.Log($"[RegisterProducer] Пойманно исключение : {ex.Message}");

                return IntPtr.Zero;
            }
        }

        //Готово
        [UnmanagedCallersOnly(EntryPoint = "RegisterProducerWithCallback", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr RegisterProducerWithCallback(IntPtr serverHost, IntPtr topicName, delegate* unmanaged[Stdcall]<IntPtr, void> unmanagedOnSentCallback)
        {
            string pinnedProducerId = string.Empty;

            try
            {
                Span<IntPtr> pointerSpan = [serverHost, topicName];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель на параметр");

                if (unmanagedOnSentCallback == null) throw new ArgumentException("Передан пустой указатель на метод");

                var server = Marshal.PtrToStringAnsi(serverHost);

                var topic = Marshal.PtrToStringAnsi(topicName);

                Action<string> managedOnSentCallback = (messageInfo) =>
                {
                    IntPtr unmanagedMessage = Marshal.StringToHGlobalAnsi(messageInfo);

                    try
                    {
                        unmanagedOnSentCallback(unmanagedMessage);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(unmanagedMessage);
                    }
                };

                var producerId = _brokerService.RegisterProducer(server!, topic!, managedOnSentCallback);

                pinnedProducerId = producerId;

                _producerCallbacks[producerId] = managedOnSentCallback;

                var resultPointer = Marshal.StringToCoTaskMemAnsi(producerId);

                return resultPointer;
            }
            catch (Exception ex)
            {
                Logger.Log($"[RegisterProducerWithCallback] Пойманно исключение : {ex.Message}");

                if (!string.IsNullOrEmpty(pinnedProducerId))
                {
                    var successfulDeletion = _producerCallbacks.TryRemove(pinnedProducerId, out _);

                    if (successfulDeletion)
                    {
                        Logger.Log($"Успешно удален коллбек для продьюссера с id: {pinnedProducerId}");
                    }
                    else
                    {
                        Logger.Log($"Коллбек для продьюссера с id: {pinnedProducerId} не был удалён, возможна утечка памяти");
                    }
                }

                return IntPtr.Zero;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "RegisterConsumerWithChannel", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr RegisterConsumerWithChannel(IntPtr serverHost, IntPtr groupName, IntPtr topicName)
        {
            try
            {
                Span<IntPtr> pointerSpan = [serverHost, groupName, topicName];

                if (pointerSpan.HasNull()) throw new ArgumentException("Передан пустой указатель");

                var server = Marshal.PtrToStringAnsi(serverHost);

                var group = Marshal.PtrToStringAnsi(groupName);

                var topic = Marshal.PtrToStringAnsi(topicName);

                var consumerId = _brokerService.RegisterConsumer(server!, group!, topic!);

                var resultPointer = Marshal.StringToCoTaskMemAnsi(consumerId);

                return resultPointer;

            }
            catch(Exception ex)
            {
                Logger.Log($"[RegisterConsumerWithChannel] Пойманно исключение : {ex.Message}");

                return IntPtr.Zero;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "RegisterConsumerWithCallback", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr RegisterConsumerWithCallback(IntPtr serverHost, IntPtr groupName, IntPtr topicName, )


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

                var consumerId = _brokerService.AddConsumer(serverHost!, groupName!, topicName!);

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
                _ = _brokerService.StartServiceAsync(() =>
                {
                    _startIsBlocked = false;

                    Logger.Log("Блокировка на старт сервиса снята через коллбэк");
                });

                var ptr = Marshal.StringToCoTaskMemAnsi("ok");

                return ptr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex.Message);

                var ptr = Marshal.StringToCoTaskMemAnsi("err");

                _startIsBlocked = false;

                Logger.Log("Блокировка на старт сервиса снята");

                return ptr;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "StopService", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr Stop()
        {
            try
            {
                _brokerService.StopService();

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

                var success = _brokerService.InputData(message);

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
