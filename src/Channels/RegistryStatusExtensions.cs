using System;
using System.Collections.Generic;
using System.Text;
using static Sufficit.Utils;

namespace Sufficit.Asterisk.Channels
{
    public static class RegistryStatusExtensions
    {
        /// <summary>
        /// "@" for invalid keys
        /// </summary>
        public static string GetKey (this RegistryStatus source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.Username) || string.IsNullOrWhiteSpace(source.Domain))
                return "@";
            
            // asterisk internally do a trim() for usernames
            var username = source.Username.Trim();

            return $"{DoubleQuote(username)}@{DoubleQuote(source.Domain)}";
        }
    }
}
