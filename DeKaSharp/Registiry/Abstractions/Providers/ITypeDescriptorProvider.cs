using DeKaSharp.Types;
using System.Collections.Frozen;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface ITypeDescriptorProvider : IDataProvider
    {
        public DataType GetTypeDescriptor<TType>();

        public FrozenDictionary<Type, DataType> GetAllDataTypes();
    }
}
