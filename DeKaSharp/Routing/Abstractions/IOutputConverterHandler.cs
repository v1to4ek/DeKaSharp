namespace DeKaSharp.Routing.Abstractions
{
    internal interface IOutputConverterHandler
    {
        public IOutputConverter<TData> GetOutputConverter<TData>();
    }
}
