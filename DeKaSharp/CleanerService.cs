namespace DeKaSharp
{
    internal interface IAsyncCleanable
    {
        public int CleanerPriority => 0;

        public Task CleanAsync();
    }

    internal class CleanerService
    {
        private readonly SortedDictionary<int, List<IAsyncCleanable>> _itemsToClean;

        public CleanerService()
        {
            var descComparer = Comparer<int>.Create((x, y) => y.CompareTo(x));

            _itemsToClean = new(descComparer);
        }

        public void RegisterItem(IAsyncCleanable item)
        {
            var priority = item.CleanerPriority;

            if(_itemsToClean.TryGetValue(priority, out List<IAsyncCleanable>? items))
            {
                items.Add(item);
            }
            else
            {
                _itemsToClean.Add(priority, [item]);
            }

            Logger.Log($"Экземпляр {item.GetType().Name} зарегистрирован для очистки с приоритетом {priority}");
        }

        public async Task CleanInstancesParallelAsync()
        {
            if (_itemsToClean.Count == 0)
            {
                Logger.Log("Нет зарегистрированных экземпляров для очистки");
                return;
            }

            foreach (var item in _itemsToClean)
            {
                var cleanables = item.Value;

                var cleaningTasks = Task.WhenAll(cleanables.Select(item => item.CleanAsync()));

                try
                {
                    await cleaningTasks;
                }
                catch 
                {
                    AggregateException exceptions = cleaningTasks.Exception!;

                    foreach (var ex in exceptions.InnerExceptions)
                    {
                        Logger.Log($"Ошибка при очистке экземпляра: {ex.Message}");
                    }
                }
            }

            Logger.Log("Очистка экземпляров завершена");

            _itemsToClean.Clear();

            Logger.Log("Список экземпляров для очистки очищен");
        }
    }
}
