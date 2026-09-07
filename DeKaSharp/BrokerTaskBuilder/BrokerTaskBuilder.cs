using DeKaSharp.BrokerTaskBuilder.Containers;

namespace DeKaSharp.BrokerTaskBuilder
{
    internal class BrokerTaskBuilder
    {
        private readonly List<IBrokerTaskContainer> _taskContainers;

        private readonly Lock _locker;

        public BrokerTaskBuilder()
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

        public IEnumerable<(bool, Func<Task>?, string?)> BuildTasks()
        {
            foreach (var container in _taskContainers)
            {
                if (container == null)
                {
                    yield return (false, null, "Контейнер равен null");

                    continue;
                }

                var taskFactory = container.GetTaskAction();

                Logger.Log($"Создана фабрика задачи для id: {container.Id}");

                yield return (true, taskFactory, null);
            }

            _taskContainers.Clear();

            Logger.Log("Все фабрики переданы, список контейнеров очищен.");
        }
    }
}
