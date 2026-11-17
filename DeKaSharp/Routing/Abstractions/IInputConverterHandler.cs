namespace DeKaSharp.Routing.Abstractions
{
    internal interface IInputConverterHandler
    {
        public IInputConverter<TData> GetInputConverter<TData>();
    }
}
