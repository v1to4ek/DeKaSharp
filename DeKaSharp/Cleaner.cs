namespace DeKaSharp
{
    internal interface IAsyncCleanable
    {
        public int CleanerPriority => 0;

        public Task CleanAsync(Action onCleaned);
    }

    internal class Cleaner
    {
        private readonly Dictionary<int, List<IAsyncCleanable>> _itemsToClean;

        public Cleaner() => _itemsToClean = [];

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
        }
    }
}
