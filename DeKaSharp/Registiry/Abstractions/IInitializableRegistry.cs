namespace DeKaSharp.Registiry.Abstractions
{
    internal interface IInitializableRegistry : IRegistry
    {
        public void Initialize();
    }
}
