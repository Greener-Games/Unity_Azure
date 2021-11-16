using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DatabaseWrapper.Core
{
    /// <summary>
    /// Static helper methods for DatabaseWrapper.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Determines if an object is of a List type.
        /// </summary>
        /// <param name="o">Object.</param>
        /// <returns>True if the object is of a List type.</returns>
        public static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// Convert an object to a List object.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>List object.</returns>
        public static List<object> ObjectToList(object obj)
        {
            if (obj == null) return null;
            List<object> ret = new List<object>();
            IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ret.Add(enumerator.Current);
            }
            return ret;
        }

        /// <summary>
        /// Convert a object into a dictionary key/Value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ClassToDictionary(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);   
            return dictionary;
        }


        /// <summary>
        /// Deserialize JSON to an object.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Object of specified type.</returns>
        public static T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Deserialize JSON to an object.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="bytes">JSON bytes.</param>
        /// <returns>Object of specified type.</returns>
        public static T DeserializeJson<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 1) throw new ArgumentNullException(nameof(bytes));
            return DeserializeJson<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Serialize an object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Enable or disable pretty printing.</param>
        /// <returns>JSON string.</returns>
        public static string SerializeJson(object obj, bool pretty)
        {
            if (obj == null) return null;
            string json;

            if (pretty)
            {
                json = JsonConvert.SerializeObject(
                  obj,
                  Newtonsoft.Json.Formatting.Indented,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                  });
            }
            else
            {
                json = JsonConvert.SerializeObject(obj,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc
                  });
            }

            return json;
        }

        /// <summary>
        /// Determine if string contains extended characters.
        /// </summary>
        /// <param name="data">String.</param>
        /// <returns>True if string contains extended characters.</returns>
        public static bool IsExtendedCharacters(string data)
        {
            if (String.IsNullOrEmpty(data)) return false;
            foreach (char c in data)
            {
                if ((int)c > 128) return true;
            }
            return false;
        }
         
        /// <summary>
        /// Retrieve the DataType from the column type.
        /// </summary>
        /// <param name="s">String containing column type.</param>
        /// <returns>DataType.</returns>
        public static DataType DataTypeFromString(string s)
        {
            if (String.IsNullOrEmpty(s)) throw new ArgumentNullException(nameof(s));

            s = s.ToLower();
            if (s.Contains("(")) s = s.Substring(0, s.IndexOf("("));

            switch (s)
            {
                case "bigserial":               // pgsql
                case "bigint":                  // mssql
                    return DataType.Long;

                case "boolean":                 // pgsql
                case "bit":                     // mssql
                case "smallserial":             // pgsql
                case "smallest":                // pgsql
                case "tinyint":                 // mssql, mysql
                case "integer":                 // pgsql, sqlite
                case "int":                     // mssql, mysql
                case "smallint":                // mssql, mysql
                case "mediumint":               // mysql
                case "serial":                  // pgsql
                    return DataType.Int;

                case "real":                    // pgsql, sqlite
                case "double":                  // mysql
                case "double precision":        // pgsql
                case "float":                   // mysql
                    return DataType.Double;

                case "decimal":                 // mssql
                case "numeric":                 // mssql
                    return DataType.Decimal;

                case "timestamp without timezone":      // pgsql
                case "timestamp without time zone":     // pgsql
                case "time without timezone":           // pgsql
                case "time without time zone":          // pgsql
                case "time":                    // mssql, mysql
                case "date":                    // mssql, mysql
                case "datetime":                // mssql, mysql
                case "datetime2":               // mssql
                case "timestamp":               // mysql
                    return DataType.DateTime;

                case "time with timezone":              // pgsql
                case "time with time zone":             // pgsql
                case "timestamp with timezone":         // pgsql
                case "timestamp with time zone":        // pgsql
                case "datetimeoffset":          // mssql
                    return DataType.DateTimeOffset;

                case "enum":                    // mysql
                case "character":               // pgsql
                case "char":                    // mssql, mysql, pgsql
                case "text":                    // mssql, mysql, pgsql, sqlite
                case "varchar":                 // mssql, mysql, pgsql
                    return DataType.Varchar;

                case "character varying":       // pgsql
                case "nchar":
                case "ntext":
                case "nvarchar":
                    return DataType.Nvarchar;   // mssql

                case "blob":
                    return DataType.Blob;       // sqlite

                default:
                    throw new ArgumentException("Unknown DataType: " + s);
            }
        }
    }
}
