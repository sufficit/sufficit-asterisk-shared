using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Sufficit.Asterisk.Helpers
{
    public static class Helper
    {
        private static readonly ILogger _logger = AsteriskLogger.CreateLogger(typeof(Helper));
        private static CultureInfo CultureInfo => Sufficit.Asterisk.Defaults.CultureInfo;

        #region ToHexString(sbyte[]) 

        /// <summary> The hex digits used to build a hex string representation of a byte array.</summary>
        public static readonly char[] hexChar =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
        };

        /// <summary>
        ///     Converts a byte array to a hex string representing it. The hex digits are lower case.
        /// </summary>
        /// <param name="b">the byte array to convert</param>
        /// <returns> the hex representation of b</returns>
        public static string ToHexString(sbyte[] b)
        {
            var sb = new StringBuilder(b.Length*2);
            for (int i = 0; i < b.Length; i++)
            {
                sb.Append(hexChar[URShift((b[i] & 0xf0), 4)]);
                sb.Append(hexChar[b[i] & 0x0f]);
            }
            return sb.ToString();
        }

        #endregion

        #region GetInternalActionId(actionId) 

        [Obsolete("not used anymore")]
        public static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
                return actionId.Substring(0, delimiterIndex).Trim();
            return string.Empty;
        }

        #endregion

        #region StripInternalActionId(actionId) 

        [Obsolete("not used anymore")]
        public static string StripInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;

            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
            {
                if (actionId.Length > delimiterIndex + 1)
                    return actionId.Substring(delimiterIndex + 1).Trim();

                return actionId.Substring(0, delimiterIndex).Trim();
            }

            return string.Empty;
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
        [Obsolete("not used anymore")]
        public static bool IsTrue(string s)
        {
            if (s == null || s.Length == 0)
                return false;
            string sx = s.ToLower(CultureInfo);
            if (sx == "yes" || sx == "true" || sx == "y" || sx == "t" || sx == "1" || sx == "on")
                return true;
            return false;
        }

        #endregion

        #region URShift(...) 

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            return (number >> bits) + (2 << ~bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        [Obsolete("not used anymore")]
        public static int URShift(int number, long bits)
        {
            return URShift(number, (int) bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        [Obsolete("not used anymore")]
        public static long URShift(long number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            return (number >> bits) + (2L << ~bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        [Obsolete("not used anymore")]
        public static long URShift(long number, long bits)
        {
            return URShift(number, (int) bits);
        }

        #endregion

        #region ToArray(ICollection c, object[] objects) 

        /// <summary>
        ///     Obtains an array containing all the elements of the collection.
        /// </summary>
        /// <param name="objects">The array into which the elements of the collection will be stored.</param>
        /// <param name="c"></param>
        /// <returns>The array containing all the elements of the collection.</returns>
        [Obsolete("not used anymore")]
        public static object[] ToArray(ICollection c, object[] objects)
        {
            int index = 0;

            var type = objects.GetType().GetElementType();
            if (type == null) throw new Exception("cant get array element type");

            var objs = (object[]) Array.CreateInstance(type, c.Count);

            IEnumerator e = c.GetEnumerator();

            while (e.MoveNext())
                objs[index++] = e.Current;

            //If objects is smaller than c then do not return the new array in the parameter
            if (objects.Length >= c.Count)
                objs.CopyTo(objects, 0);

            return objs;
        }

        #endregion

        #region ParseVariables(Dictionary<string, string> dictionary, string variables, char[] delim)

        /// <summary>
        ///     Parse variable(s) string to dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="variables">variable(a) string</param>
        /// <param name="delim">variable pairs delimiter</param>
        /// <returns></returns>
        [Obsolete("not used anymore")]
        public static IDictionary<string, string> ParseVariables(IDictionary<string, string> dictionary,
            string variables, char[] delim)
        {
            if (dictionary == null)
                dictionary = new Dictionary<string, string>();
            else
                dictionary.Clear();

            if (string.IsNullOrEmpty(variables))
                return dictionary;
            string[] vars = variables.Split(delim);
            int idx;
            string vname, vval;
            foreach (var var in vars)
            {
                idx = var.IndexOf('=');
                if (idx > 0)
                {
                    vname = var.Substring(0, idx);
                    vval = var.Substring(idx + 1);
                }
                else
                {
                    vname = var;
                    vval = string.Empty;
                }
                dictionary.Add(vname, vval);
            }
            return dictionary;
        }

        #endregion
        #region GetMillisecondsFrom(DateTime start) 

        [Obsolete("not used anymore")]
        public static long GetMillisecondsFrom(DateTime start)
        {
            TimeSpan ts = DateTime.Now - start;
            return (long) ts.TotalMilliseconds;
        }

        #endregion

        #region ParseString(string val) 

        [Obsolete("not used anymore")]
        public static object ParseString(string val)
        {
            if (val == "none")
                return string.Empty;
            return val;
        }

        #endregion

        /// <summary>
        ///   Retorna um Mapa de métodos getter públicos declarados diretamente na classe fornecida.<br />
        ///   A chave do mapa contém o nome da propriedade que pode ser acessada pelo getter (ex: "MinhaPropriedade"), 
        ///   e o valor é o próprio MethodInfo do getter.<br />
        ///   Um método é considerado um getter se seu nome começa com "get_", não recebe argumentos, 
        ///   é público, e é declarado diretamente na classe 'clazz' (não herdado).
        /// </summary>
        /// <param name="clazz">A classe para a qual retornar os getters declarados nela.</param>
        /// <returns>Um Dicionário mapeando nomes de propriedades aos seus métodos getter públicos declarados diretamente na classe.</returns>
        public static Dictionary<string, MethodInfo> GetGetters(Type clazz)
        {
            string propertyName;
            string methodName;
            MethodInfo method;

            var accessors = new Dictionary<string, MethodInfo>();
            MethodInfo[] methods = clazz.GetMethods(
                 BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
                );

            for (int i = 0; i < methods.Length; i++)
            {
                method = methods[i];
                methodName = method.Name;

                // Critérios para um método ser considerado um getter de propriedade:
                // 1. O nome do método deve começar com "get_" (convenção do C# para getters de propriedades).
                // 2. O método não deve ter parâmetros.
                if (!methodName.StartsWith("get_") || method.GetParameters().Length != 0)                
                    continue; // Pula para o próximo método se não atender aos critérios.                

                // Extrai o nome da propriedade a partir do nome do método getter
                // (removendo o prefixo "get_").
                propertyName = methodName.Substring(4);

                // Verifica se o nome da propriedade resultante não está vazio.
                if (propertyName.Length == 0)                
                    continue; // Caso raro, mas protege contra um método chamado apenas "get_".                

                // Adiciona o nome da propriedade e o MethodInfo do getter ao dicionário.
                accessors[propertyName] = method;
            }

            return accessors;
        }

        #region ToString(object obj) 

        /// <summary>
        ///     Convert object with all properties to string
        /// </summary>
        public static string ToString(object? obj, Type type)
        {
            object? value;
            var sb = new StringBuilder(type.Name, 1024);
            sb.Append(" {");
            string? strValue;
            IDictionary getters = GetGetters(type);
            bool notFirst = false;
            var arrays = new List<MethodInfo>();
            // First step - all values properties (not a list)
            foreach (string name in getters.Keys)
            {
                var getter = (MethodInfo?)getters[name];
                if (getter == null) throw new ArgumentNullException(name);

                var propType = getter.ReturnType;
                if (propType == typeof(object))
                    continue;
                if (
                    !(propType == typeof(string) || propType == typeof(bool) || propType == typeof(double) ||
                      propType == typeof(DateTime) || propType == typeof(int) || propType == typeof(long)))
                {
                    var propTypeName = propType.Name;
                    if (propTypeName.StartsWith("Dictionary") || propTypeName.StartsWith("List"))
                    {
                        arrays.Add(getter);
                    }
                    continue;
                }

                try
                {
                    value = getter.Invoke(obj, new object[] { });
                }
                catch
                {
                    continue;
                }

                if (value == null)
                    continue;
                if (value is string)
                {
                    strValue = (string)value;
                    if (strValue.Length == 0)
                        continue;
                }
                else if (value is bool)
                {
                    strValue = ((bool)value ? "true" : "false");
                }
                else if (value is double)
                {
                    var d = (double)value;
                    if (d == 0.0)
                        continue;
                    strValue = d.ToString();
                }
                else if (value is DateTime)
                {
                    var dt = (DateTime)value;
                    if (dt == DateTime.MinValue)
                        continue;
                    strValue = dt.ToLongTimeString();
                }
                else if (value is int)
                {
                    var i = (int)value;
                    if (i == 0)
                        continue;
                    strValue = i.ToString();
                }
                else if (value is long)
                {
                    var l = (long)value;
                    if (l == 0)
                        continue;
                    strValue = l.ToString();
                }
                else
                    strValue = value.ToString();

                if (notFirst)
                    sb.Append("; ");
                notFirst = true;
                sb.Append(string.Concat(getter.Name.Substring(4), ":", strValue));
            }

            // Second step - all lists
            foreach (var getter in arrays)
            {
                value = null;
                try
                {
                    value = getter.Invoke(obj, new object[] { });
                }
                catch
                {
                    continue;
                }
                if (value == null)
                    continue;

                #region List 

                IList list;
                if (value is IList && (list = (IList)value).Count > 0)
                {
                    if (notFirst)
                        sb.Append("; ");
                    notFirst = true;
                    sb.Append(getter.Name.Substring(4));
                    sb.Append(":[");
                    bool notFirst2 = false;
                    foreach (var o in list)
                    {
                        if (notFirst2)
                            sb.Append("; ");
                        notFirst2 = true;
                        sb.Append(o);
                    }
                    sb.Append("]");
                }

                #endregion
                #region IDictionary 

                if (value is IDictionary && ((IDictionary)value).Count > 0)
                {
                    if (notFirst)
                        sb.Append("; ");
                    notFirst = true;
                    sb.Append(getter.Name.Substring(4));
                    sb.Append(":[");
                    bool notFirst2 = false;
                    foreach (var key in ((IDictionary)value).Keys)
                    {
                        var o = ((IDictionary)value)[key];
                        if (notFirst2)
                            sb.Append("; ");
                        notFirst2 = true;
                        sb.Append(string.Concat(key, ":", o));
                    }
                    sb.Append("]");
                }

                #endregion
            }

            sb.Append("}");
            return sb.ToString();
        }

        /// <inheritdoc cref="ToString(object, Type)"/>
        [Obsolete("Not Used Anymore.")]
        public static string ToString<T>(T obj)
            => ToString(obj, typeof(T));

        #endregion
    }
}
