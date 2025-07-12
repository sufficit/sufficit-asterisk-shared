using System;

namespace Sufficit.Asterisk.Helpers
{
    public static class HelperExtensions
    {
        public static object? EnumParse(this string? source, Type type)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var dataType = type;
            if (!dataType.IsEnum)                
                dataType = Nullable.GetUnderlyingType(dataType)!;                
            
            // trying with sufficit methods
            if (Sufficit.Utils.Enum.TryParse(source, dataType, true, out object? result))                
                return result;

            // trying with System.Enum default methods
            result = Enum.Parse(dataType, source, true);
            return System.Convert.ChangeType(result, dataType);
        }
    }
}
