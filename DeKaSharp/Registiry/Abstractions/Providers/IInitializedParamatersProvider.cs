using DeKaSharp.BrokerClientCore.Parameters;

namespace DeKaSharp.Registiry.Abstractions.Providers
{
    internal interface IInitializedParamatersProvider : IDataProvider
    {
        public IReadOnlyDictionary<Enum, Parameter<TValue>> GetInitializedParameters<TValue>();
    }
}
