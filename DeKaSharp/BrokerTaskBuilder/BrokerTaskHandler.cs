using DeKaSharp.BrokerTaskBuilder.Containers;

namespace DeKaSharp.BrokerTaskBuilder
{
    internal enum BrokerTaskType
    {
        Producer,
        Consumer,
        Undefined
    }
    internal record class BuildedResult
    (bool IsSuccess,
    string? Id,
    BrokerTaskType Type,
    Func<Task>? TaskFactory,
    string? ErrorMessage);

    internal class BrokerTaskHandler
    {
        private readonly List<IBrokerTaskContainer> _taskContainers;

        private readonly Lock _locker;

        public BrokerTaskHandler()
        {
            _taskContainers = [];

            _locker = new ();
        }

        public void AddTaskContainer<T> (Func<T> taskContainerFactory)
            where T : IBrokerTaskContainer
        {
            var container = taskContainerFactory();

            lock (_locker)
            {
                _taskContainers.Add(container);

                Logger.Log($"Добавлен контейнер для задачи с id: {container.Id}");
            }
        }

        public IEnumerable<BuildedResult> BuildTasks()
        {
            lock (_locker)
            {
                var results = new List<BuildedResult>();

                foreach (var container in _taskContainers)
                {
                    var taskFactory = container.GetTaskAction();

                    Logger.Log($"Создана фабрика задачи для id: {container.Id}");

                    results.Add(new BuildedResult(true,
                        container.Id,
                        container.Type,
                        taskFactory,
                        null));
                }

                Logger.Log("Все фабрики сгенерированы.");

                return results;
            }
        }


    }
}
