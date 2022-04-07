using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using Newtonsoft.Json;

namespace DatabaseWrapper.Core
{
    public static class DataTableExtensions
    {
               /// <summary>
        /// Determine if a DataTable is null or has no rows.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>True if DataTable is null or has no rows.</returns>
        public static bool IsNullOrEmpty(this DataTable table)
        {
            if (table == null) return true;
            if (table.Rows.Count < 1) return true;
            return false;
        }
        
        /// <summary>
        /// Convert a DataTable to an object.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="table">DataTable.</param>
        /// <returns>Object of specified type.</returns>
        public static T ToObject<T>(this DataTable table) where T : new()
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (table.Rows.Count < 1) throw new ArgumentException("No rows in DataTable");
            foreach (DataRow r in table.Rows)
            {
                return r.ToObject<T>();
            }
            return default(T);
        }
        
        
        /// <summary>
        /// Convert a DataTable to a List of objects.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>List of dynamic objects.</returns>
        public static List<T> ToListOfObject<T>(this DataTable table) where T : new()
        {
            List<T> ret = new List<T>();
            if (table == null || table.Rows.Count < 1) return ret;
            
            foreach (DataRow r in table.Rows)
            {
                ret.Add(r.ToObject<T>());
            }
            
            return ret;
        }
        
        /// <summary>
        /// Convert a DataTable to a List of dynamic objects.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>List of dynamic objects.</returns>
        public static List<dynamic> ToListDynamic(this DataTable table)
        {
            List<dynamic> ret = new List<dynamic>();
            if (table == null || table.Rows.Count < 1) return ret;

            foreach (DataRow curr in table.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in table.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

        /// <summary>
        /// Convert a DataTable to a dynamic object.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>Dynamic object.</returns>
        public static dynamic ToDynamic(this DataTable table)
        {
            dynamic ret = new ExpandoObject();
            if (table == null || table.Rows.Count < 1) return ret;
            if (table.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Convert a DataTable to a List Dictionary.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>List Dictionary.</returns>
        public static List<Dictionary<string, object>> ToListDictionary(this DataTable table)
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            if (table == null || table.Rows.Count < 1) return ret;

            foreach (DataRow curr in table.Rows)
            {
                Dictionary<string, object> currDict = new Dictionary<string, object>();

                foreach (DataColumn col in table.Columns)
                {
                    currDict.Add(col.ColumnName, curr[col]);
                }

                ret.Add(currDict);
            }

            return ret;
        }

        /// <summary>
        /// Convert a DataTable to a Dictionary.
        /// </summary>
        /// <param name="table">DataTable.</param>
        /// <returns>Dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(this DataTable table)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (table == null || table.Rows.Count < 1) return ret;
            if (table.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    ret.Add(col.ColumnName, curr[col]);
                }

                return ret;
            }

            return ret;
        }
        
        public static string ToJson(DataTable table)
        {
            List<Dictionary<string, object>> rows = table.ToListDictionary();
            return JsonConvert.SerializeObject(rows);
        }
    }
}