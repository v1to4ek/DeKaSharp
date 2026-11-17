using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.DataObjects;
using DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators.Enums;

namespace DeKaSharp.BrokerClientCore.Configurators.BuilderConfigurators
{
    internal static class BuilderParametersValidatorHelper
    {
        //возможно стоит добавть аналог шаблона для String значений в ParameterRequirement, чтобы проверять, соответствует ли полученный параметр этому шаблону
        public static bool CheckValue(InnerBuilderParameter parameter, Dictionary<string, ParameterRequirement> requirements)
        {
            var value = parameter.Value;

            var format = requirements[parameter.Key].FormatTypeRequirement;

            if (format == ParameterRequiredFormat.Boolean)
            {
                if (value == "TRUE" || value == "FALSE") return true;
            }

            if (format == ParameterRequiredFormat.Integer)
            {
                if (int.TryParse(value, out var _)) return true;
            }

            if (format == ParameterRequiredFormat.String)
            {
                if (value != "") return true;
            }

            return false;
        }

        //возможно при нескольких значениях у одного ключа, стоит добавить максимальное доступное количество значений по этому ключу, если такое необходимо
        public static void CheckMultipleKeys(Dictionary<string, int> countedFoundKeys, Dictionary<string, ParameterRequirement> requirements)
        {
            List<string>? errorKeys = null;

            foreach (var kv in countedFoundKeys.Where(kv => kv.Value > 1))
            {
                var key = kv.Key;

                var requirement = requirements[key];

                if (requirement.MultipleValuesRequirement == ParameterAllowMultipleValues.False)
                {
                    errorKeys ??= [];

                    errorKeys.Add(key);
                }
            }

            if (errorKeys != null) throw new ArgumentException();
        }

        public static void CheckMissedKeys(HashSet<string> missedKeys, Dictionary<string, ParameterRequirement> requirements)
        {
            List<string>? missedRequiredKeys = null;

            foreach (var key in missedKeys)
            {
                var requirement = requirements[key];

                if (requirement.NoValueRequirement == ParameterValueRequired.True)
                {
                    missedRequiredKeys ??= [];

                    missedRequiredKeys.Add(key);
                }
            }

            if (missedRequiredKeys != null) throw new ArgumentException();
        }
    }
}
