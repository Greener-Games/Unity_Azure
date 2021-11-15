#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Atkins.AzureHelpers;
using Newtonsoft.Json;
using UnityEngine;

#endregion

namespace Atkins.AzureHelpers
{
    public class AzureDatabase : MonoBehaviour
    {
        
        // Start is called before the first frame update
        void Start()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = AzureSettings.Asset.dataSource;
                builder.UserID = AzureSettings.Asset.databaseUserId;
                builder.Password = AzureSettings.Asset.databasePassword;
                builder.InitialCatalog = AzureSettings.Asset.databaseName;

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    string sql = "SELECT * from TestTable";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        string json = ExecuteToJson(command);
                        Debug.Log(json);
                    }
                }
            }
            catch (SqlException e)
            {
                Debug.Log(e.ToString());
            }

            Debug.Log("\nDone. Press enter.");
        }

        public static string ExecuteToJson(SqlCommand cmd)
        {
            if (cmd.Connection.State == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }

            using (DataTable dt = new DataTable())
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);

                    List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                    Dictionary<string, object> row;
                    foreach (DataRow dr in dt.Rows)
                    {
                        row = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            row.Add(col.ColumnName, dr[col]);
                        }

                        rows.Add(row);
                    }

                    return JsonConvert.SerializeObject(rows);
                }
            }
        }
    }
}