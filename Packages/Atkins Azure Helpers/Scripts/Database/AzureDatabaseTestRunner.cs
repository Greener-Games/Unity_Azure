#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Atkins.AzureHelpers;
using DatabaseWrapper.Core;
using DatabaseWrapper.SqlServer;
using Newtonsoft.Json;
using UnityEngine;

#endregion

namespace Atkins.AzureHelpers
{
    public class AzureDatabaseTestRunner : MonoBehaviour
    {
        public class User
        {
            public string Title { get; set; }
            public int Value { get; set; }
        }
        
        // Start is called before the first frame update
        void Start()
        {
            //create settings
            DatabaseSettings settings = new DatabaseSettings()
            {
                Type = DbTypes.SqlServer,
                Hostname = AzureSettings.Asset.dataSource,
                DatabaseName = AzureSettings.Asset.databaseName,
                Username = AzureSettings.Asset.databaseUserId,
                Password = AzureSettings.Asset.databasePassword,
                Port = 1433,
                
                LogResults = true,
                LogQueries = true,
                Logger = Debug.Log,
            };

            DatabaseClient client = new DatabaseClient(settings);
            
            DataTable result = client.Select("TestTable", null, null, null, null);
            List<User> users = result.ToListOfObject<User>();
            Debug.Log(users[0].Title);

            result = client.Select("TestTable", null, null, null, new Expression("Title", Operators.Equals, "Title1"));
            User user = result.ToObject<User>();
            Debug.Log(user.Title);

            User u = new User
            {
                Title = "Upload1",
                Value = 100
            };

            client.Upsert("TestTable", u, new Expression("Title", Operators.Equals, u.Title));
        }
    }
}