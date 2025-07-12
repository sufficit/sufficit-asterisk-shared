using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager;
using System;
using System.Globalization;

namespace Sufficit.Asterisk.Helpers
{
    public static class FromStringBuilderHelper
    {
        private static readonly ILogger _logger = AsteriskLogger.CreateLogger(typeof(FromStringBuilderHelper));
        private static CultureInfo CultureInfo => Sufficit.Asterisk.Defaults.CultureInfo;


        public static bool TryConvert(Type type, string? value, string title, Type sourceType, out object? Result)
        {
            try
            {
                Result = Convert(type, value, title, sourceType);
                return true;
            }
            catch (Exception ex)
            {
                Result = null;
                _logger.LogError(ex, "error on TryConvert");
                return false;
            }
        }

        public static bool IsTypeOrNullableOf<T>(Type match)
        {
            var type = typeof(T);
            if (match == type || Nullable.GetUnderlyingType(match) == type) return true;
            else return false;
        }

        public static bool IsNullableOf<T>(Type match)
        {
            var type = typeof(T);
            if (Nullable.GetUnderlyingType(match) == type) return true;
            else return false;
        }

        public static bool IsEnumOrNullableOf(Type match)
        {
            if (match.IsEnum)
            {
                return true;
            } else {
                var type = Nullable.GetUnderlyingType(match);
                if(type != null && type.IsEnum) return true;
            }    

            return false; 
        }

        public static object? Convert(Type type, string? value, string title, Type eventType)
        {
            if (IsTypeOrNullableOf<bool>(type))
                return IsTrue(value);
            else if (IsTypeOrNullableOf<string>(type))
                return ParseString(value);
            else if (IsTypeOrNullableOf<uint>(type))
            {
                uint.TryParse(value, out uint v);
                return v;
            }
            else if (IsTypeOrNullableOf<Int32>(type))
            {
                Int32.TryParse(value, out Int32 v);
                return v;
            }
            else if (IsTypeOrNullableOf<Int64>(type))
            {
                Int64.TryParse(value, out Int64 v);
                return v;
            }
            else if (IsTypeOrNullableOf<double>(type))
            {
                Double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo, out Double v);
                return v;
            }
            else if (IsTypeOrNullableOf<decimal>(type))
            {
                Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo, out Decimal v);
                return v;
            } 
            else if (IsTypeOrNullableOf<DateTime>(type))
            {
                if (IsNullableOf<DateTime>(type) && string.IsNullOrWhiteSpace(value))
                    return null;

                DateTime.TryParse(value, out DateTime v);
                return v;
            }
            else if (IsTypeOrNullableOf<TimeSpan>(type))
            {
                if (IsNullableOf<TimeSpan>(type) && string.IsNullOrWhiteSpace(value))
                    return null;

                TimeSpan.TryParse(value, out TimeSpan v);
                return v;
            }
            else if (IsEnumOrNullableOf(type))
            {
                try
                {
                    return value.EnumParse(type);
                }
                catch (Exception ex)
                {
                    var errorString = "Unable to convert value '" + value + "' of property '" + title + "' on " + eventType + " to required enum type " + type;
                    _logger.LogError(ex, errorString);
					throw new AsteriskManagerException(errorString, ex); 
                }
            }
            else
            {
                try
                {
                    var constructor = type.GetConstructor(new[] { typeof(string) });
                    return constructor?.Invoke(new object?[] { value });
                }
                catch (Exception ex)
                {
                    var errorString = "Unable to convert value '" + value + "' of property '" + title + "' on " + eventType + " to required type " + type;
                    _logger.LogError(ex, errorString);
    				throw new AsteriskManagerException(errorString, ex);
                }
            }
        }

        #region ParseString(string val) 

        internal static object? ParseString(string? val)
        {
            if (val == "none")
                return string.Empty;
            return val;
        }

        #endregion

        #region IsTrue(string) 

        /// <summary>
        ///     Checks if a String represents true or false according to Asterisk's logic.<br />
        ///     The original implementation is util.c is as follows:
        /// </summary>
        /// <param name="s">the String to check for true.</param>
        /// <returns>
        ///     true if s represents true,
        ///     false otherwise.
        /// </returns>
        internal static bool IsTrue(string? s)
        {
            if (s == null || s.Length == 0)
                return false;
            string sx = s.ToLower(CultureInfo);
            if (sx == "yes" || sx == "true" || sx == "y" || sx == "t" || sx == "1" || sx == "on")
                return true;
            return false;
        }

        #endregion
    }
}
