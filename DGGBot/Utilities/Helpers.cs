using System;
using System.Runtime.InteropServices;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DGGBot.Utilities
{
    public static class Helpers
    {
        public static JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(true, false)
                },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public static TimeZoneInfo CentralTimeZone()
        {
            return TimeZoneInfo.FindSystemTimeZoneById(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Central Standard Time"
                : "America/Chicago");
        }

        public static Color GetColorFromHex(string hexString)
        {

            if (!System.Text.RegularExpressions.Regex.IsMatch(hexString, @"[#]([0-9]|[a-f]|[A-F]){6}\b"))
                throw new ArgumentException();

            //remove the # at the front
            hexString = hexString.Replace("#", String.Empty);

            
            byte r = 255;
            byte g = 255;
            byte b = 255;

            var start = 0;

            //handle ARGB strings (8 characters long)
            if (hexString.Length == 8)
            {
               throw new ArgumentException("Hex color can only be six characters");
            }

            //convert RGB characters to bytes
            r = byte.Parse(hexString.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hexString.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hexString.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r, g, b);
        }
    }
}