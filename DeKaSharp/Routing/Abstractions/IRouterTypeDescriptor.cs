using DeKaSharp.Routing.Enums;

namespace DeKaSharp.Routing.Abstractions
{
    internal interface IRouterTypeDescriptor
    {
        static abstract RouterType RouterType { get; }
    }
}
