namespace DeKaSharp.BrokerClientCore.Parameters
{
    internal record class Parameter<TValue>
    {
        public Enum Name { get; }

        public TValue? Value { get; set; }

        public bool HasDefalutValue { get; }

        public Parameter(Enum name, TValue? value, bool hasDefalutValue)
        {
            Name = name;

            Value = value;

            HasDefalutValue = hasDefalutValue;
        }
    }
}
