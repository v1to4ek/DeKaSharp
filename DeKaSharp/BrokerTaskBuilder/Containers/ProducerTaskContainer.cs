using Dekaf.Producer;

namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal class ProducerTaskContainer : IBrokerTaskContainer
    {
        private readonly string _id;

        private readonly string _topic;

        private readonly IKafkaProducer<string, string> _producer;

        private readonly ChannelRouter _router;

        private readonly CancellationToken _ct;

        private event Action<string>? OnSentCallback;

        private event Action<string>? OnErrorCallback;

        public string Id => _id;

        public BrokerTaskType Type => BrokerTaskType.Producer;

        public ProducerTaskContainer(string id,
            string topic,
            IKafkaProducer<string, string> producer,
            ChannelRouter router,
            CancellationToken ct)
        {
            _id = id;

            _topic = topic;

            _producer = producer;

            _router = router;

            _ct = ct;

            Logger.Log($"Создан контейнер продьюсера c id: {_id}");
        }

        public ProducerTaskContainer(string id,
            string topic,
            IKafkaProducer<string, string> producer,
            ChannelRouter router,
            Action<string> onSentCallback,
            CancellationToken ct) 
            : this(id, topic ,producer, router, ct)
            => OnSentCallback += onSentCallback;

        public void SubscribeToErrorEvent(Action<string>? onErrorCallback) => OnErrorCallback += onErrorCallback;

        //можно передать коллбэк для возврата ошибки
        //можно добавить вариант чтения из коллбека, а не из канала
        public Func<Task> GetTaskAction()
            => () => Task.Run(
                async () =>
                {
                    Logger.Log($"Вход в асинхронную задачу продьюсера с id: {_id} ");

                    try
                    {
                        var (channelExists, channel) = _router.GetChannelById(_id);

                        if (!channelExists)
                        {
                            throw new Exception($"Канал с id: {_id} не найден. Продьюсер не может быть запущен");
                        }

                        await foreach (var item in channel!.Reader.ReadAllAsync(_ct))
                        {
                            var mesKey = item.MessageKey;

                            var mesValue = item.MessageValue;

                            var data = await _producer.ProduceAsync(_topic, mesKey, mesValue);

                            OnSentCallback?.Invoke($"Отправлено сообщение. Время: {data.Timestamp}. Топик: {data.Topic}");

                            Logger.Log($"Отправлено сообщение. Время: {data.Timestamp}. Топик: {data.Topic}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log($"Задача продьюсера с id: {_id} остановлена");

                        throw;
                    }
                    catch (Exception ex)
                    {
                        OnErrorCallback?.Invoke($"Поймано исключение в таске продьюсера c id: {_id} : {ex.Message}");

                        Logger.Log($"Поймано исключение в таске продьюсера c id: {_id} : {ex.Message}");
                    }
                },
                _ct);

        public async Task CleanAsync()
        {
            await _producer.FlushAsync().AsTask();

            await _producer.DisposeAsync().AsTask();

            OnSentCallback = null;

            OnErrorCallback = null;

            Logger.Log($"Очистка контейнера продьюсера c id: {_id} завершена");
        }
    }
}
