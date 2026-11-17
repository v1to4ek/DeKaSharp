namespace DeKaSharp.Routing.Abstractions
{
    internal interface IInputConverter<TData> : IPointerConverter
    {
        public TData Convert(nint data);
    }
}
