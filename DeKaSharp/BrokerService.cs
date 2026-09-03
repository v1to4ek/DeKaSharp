using Dekaf.Consumer;
using Dekaf.Producer;

namespace DeKaSharp
{
    internal class BrokerService
    {
        public BrokerService()
        {

        }

        public string RegisterProducer()
        {

        }

        public string RegisterConsumer()
        {

        }

        private void AddProducerTask()
        {

        }

        private void AddConsumerTask()
        {

        }
        public async Task StartServiceAsync()
        {

        }

        public void StopService()
        {

        }

        public void ProduceMessage()
        {

        }

        public unsafe void SetConsumerCallback(delegate*<string,string> callback)
        {

        }
    }

    internal class TaskBuilder
    {
        private readonly List<ITaskContainer> _taskContainers;

        private readonly CancellationToken _ct;

        private class ProducerTaskActionContainer : ITaskContainer
        {
            private readonly string _id;

            private readonly string _topic;

            private readonly IKafkaProducer<string, string> _producer;

            private readonly ProducingRouter _router;

            private readonly CancellationToken _ct;

            public ProducerTaskActionContainer(string id,
                string topic, 
                IKafkaProducer<string,string> producer,
                ProducingRouter router,
                CancellationToken ct)
            {
                _id = id;

                _topic = topic;

                _producer = producer;

                _router = router;

                _ct = ct;

                Logger.Log($"Создан контейнер продьюсера c id: {_id}");
            }

            //можно передать коллбэк для возврата ошибки
            //можно добавить вариант чтения из коллбека, а не из канала
            public Func<Task> GetTaskAction()
                => () => Task
                .Factory
                .StartNew(async () =>
                {
                    try
                    {
                        var channel = _router.GetChannelById(_id);

                        await foreach (var item in channel.Reader.ReadAllAsync(_ct))
                        {
                            var mesKey = item.MessageKey;

                            var mesValue = item.MessageValue;

                            var data = await _producer.ProduceAsync(_topic, mesKey, mesValue);

                            Logger.Log($"Отправлено сообщение. Время: {data.Timestamp}. Топик: {data.Topic}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Поймано исключение в таске продьюсера c id: {_id} : {ex.Message}");
                    }
                },
                _ct,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        private class ConsumerTaskContainer : ITaskContainer
        {
            private Action<string, string>? _callback;

            private readonly string _id;

            private readonly IKafkaConsumer<string,string> _consumer;

            private readonly CancellationToken _ct;

            public ConsumerTaskContainer(string id, 
                string topicName,
                IKafkaConsumer<string,string> consumer,
                CancellationToken ct)
            {
                _id = id;

                _consumer = consumer;

                _ct = ct;

                Logger.Log($"Создан контейнер консьюмера c id: {_id}");
            }

            //можно передать коллбэк для возврата ошибки
            //можно добавить вариант возврата значеня не через коллбэк, а писать к примеру в канал, который будет читаться в другом месте
            public Func<Task> GetTaskAction()
                => () => Task
                .Factory
                .StartNew(async () =>
                {
                    try
                    {
                        if (_callback is null) throw new Exception($"Не задан коллбэк для консьюмера с id: {_id}");

                        await foreach (var message in _consumer.ConsumeAsync(_ct))
                        {
                            var messageKey = message.Key ?? "null data";

                            var messageValue = message.Value ?? "null data";

                            _callback.Invoke(messageKey, messageValue);

                            Logger.Log($"Получено сообщение. Время: {message.Timestamp}. Топик: {message.Topic}. Ключ: {messageKey}. Значение: {messageValue}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Поймано исключение в таске консъюмера c id: {_id} : {ex.Message}");
                    }
                },
                _ct,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();


            public void SetCallback(Action<string, string> callback) => _callback = callback;

        }

        public TaskBuilder(CancellationToken ct)
        {
            _taskContainers = [];

            _ct = ct;
        }


        //попробовать сделать обобщённый метод
        public void AddProducerTask(string id)
        {
            _taskContainers.Add(new ProducerTaskActionContainer(, _ct));
        }

        public void AddConsumerTask(string id)
        {
            _taskContainers.Add(new ConsumerTaskContainer(, _ct));
        }

        public IEnumerable<(Task,bool,string)> BuildTasks()
        {
            
        }
    }

    internal interface ITaskContainer
    {
        public Func<Task> GetTaskAction();
    }
}
