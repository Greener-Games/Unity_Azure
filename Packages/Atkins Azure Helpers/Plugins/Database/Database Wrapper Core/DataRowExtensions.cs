using System;
using System.Data;
using System.Reflection;

namespace DatabaseWrapper.Core
{
    public static class DataRowExtensions
    {
        /// <summary>
        /// Convert a DataRow to an object.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="row">DataRow.</param>
        /// <returns>Object of specified type.</returns>
        public static T ToObject<T>(this DataRow row) where T : new()
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            T item = new T();
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                property.SetValue(item, row[property.Name], null);
            }
            return item;
        }
    }
}