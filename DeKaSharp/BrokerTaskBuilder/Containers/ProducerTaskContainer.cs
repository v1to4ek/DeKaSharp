using Dekaf.Errors;
using Dekaf.Producer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp.BrokerTaskBuilder.Containers
{
    internal class ProducerTaskContainer : IBrokerTaskContainer
    {
        private readonly string _id;

        private readonly string _topic;

        private readonly IKafkaProducer<string, string> _producer;

        private readonly InputRouter _router;

        private readonly CancellationToken _ct;

        public string Id => _id;

        public BrokerTaskType Type => BrokerTaskType.Producer;

        public ProducerTaskContainer(string id,
            string topic,
            IKafkaProducer<string, string> producer,
            InputRouter router,
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
            => () => Task.Run(
                async () =>
                {
                    Logger.Log($"Вход в асинхронную задачу продьюсера с id: {_id} ");

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
                    catch (ProduceException ex)
                    {
                        Logger.Log($"Поймано исключение в таске продьюсера c id: {_id} : {ex.Message}");
                    }
                },
                _ct);

    }
}
