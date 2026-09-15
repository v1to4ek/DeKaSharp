using Dekaf.Consumer;

namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal class ConsumerTaskContainer : IBrokerTaskContainer
    {
        private readonly string _id;

        private readonly IKafkaConsumer<string, string> _consumer;

        private readonly CancellationToken _ct;

        private readonly ChannelRouter? _outputRouter;

        private event Action<string, string>? OutputCallback;

        private event Action<string>? OnErrorCallback;

        public string Id => _id;

        public BrokerTaskType Type => BrokerTaskType.Consumer;

        public ConsumerTaskContainer(string id,
            IKafkaConsumer<string, string> consumer,
            Action<string, string> outputCallback,
            CancellationToken ct)
        {
            _id = id;

            _consumer = consumer;

            OutputCallback += outputCallback;

            _ct = ct;

            Logger.Log($"Создан контейнер коллбэк-консъюмера c id: {_id}");
        }

        public ConsumerTaskContainer(string id,
            IKafkaConsumer<string,string> consumer,
            ChannelRouter router,
            CancellationToken ct)
        {
            _id = id;

            _consumer = consumer;

            _outputRouter = router;

            _outputRouter.RegisterChannel(_id);

            _ct = ct;

            Logger.Log($"Создан контейнер канал-консъюмера c id: {_id}");
        }

        public void SubscribeToErrorEvent(Action<string>? onErrorCallback) => OnErrorCallback += onErrorCallback;

        //можно передать коллбэк для возврата ошибки
        //можно добавить вариант возврата значеня не через коллбэк, а писать к примеру в канал, который будет читаться в другом месте
        public Func<Task> GetTaskAction()
            => () => Task.Run(
                async () =>
                {
                    Logger.Log($"Вход в асинхронную задачу консъюмера с id: {_id} ");

                    try
                    {
                        await foreach (var message in _consumer.ConsumeAsync(_ct))
                        {
                            var messageKey = message.Key ?? "null data";

                            var messageValue = message.Value ?? "null data";

                            if(_outputRouter != null)
                            {
                                await _outputRouter.PublishItemAsync(new InputMessage(_id, messageKey, messageValue), _ct);
                            }

                            OutputCallback?.Invoke(messageKey, messageValue);

                            Logger.Log($"Получено сообщение. Время: {message.Timestamp}. Топик: {message.Topic}. Ключ: {messageKey}. Значение: {messageValue}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log($"Задача консъюмера с id: {_id} остановлена");

                        throw;
                    }
                    catch (Exception ex)
                    {
                        OnErrorCallback?.Invoke($"Поймано исключение в таске консъюмера c id: {_id} : {ex.Message}");

                        Logger.Log($"Поймано исключение в таске консъюмера c id: {_id} : {ex.Message}");
                    }
                },
                _ct);

        public async Task CleanAsync()
        {
            await _consumer.CloseAsync().AsTask();

            await _consumer.DisposeAsync().AsTask();

            OutputCallback = null;

            OnErrorCallback = null;

            Logger.Log($"Очистка контейнера консъюмера c id: {_id} завершена");
        }
    }
}
