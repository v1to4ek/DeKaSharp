namespace DeKaSharp.Routing.Abstractions
{
    internal interface IOutputConverter<TData> : IPointerConverter
    {
        public nint Convert(TData data);
    }
}
