using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BatteryStatsCollectionWorkerService
{
   
        public static class DataAccess
        {
            private static string getDirectory()
            {
                string dir = "C:\\BatteryPro";

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return dir;
            }
            public async static void InitializeDatabase()
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(getDirectory());
                await folder.CreateFileAsync("batteryPro.db", CreationCollisionOption.OpenIfExists);
                string dbpath = Path.Combine(getDirectory(), "batteryPro.db");
                using SqliteConnection db = new($"Filename={dbpath}");
                db.Open();

                String tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS BatteryStats (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "batteryLevel FLOAT , " +
                    "isCharging BOOLEAN , " +
                    "timeStamp VARCHAR(50) )";

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }

            public static void AddData(string batteryLevel, bool status, string timeStamp)
            {
            
                string dbpath = Path.Combine(getDirectory(), "batteryPro.db");
                using (SqliteConnection db =
                  new SqliteConnection($"Filename={dbpath}"))
                {
                    db.Open();

                SqliteCommand insertCommand = new()
                {
                    Connection = db,

                    // Use parameterized query to prevent SQL injection attacks
                    CommandText = "INSERT INTO BatteryStats VALUES (NULL, @batteryLevel, @status, @timeStamp);"
                };
                insertCommand.Parameters.AddWithValue("@batteryLevel", batteryLevel);
                    insertCommand.Parameters.AddWithValue("@status", status);
                    insertCommand.Parameters.AddWithValue("@timeStamp", timeStamp);

                    insertCommand.ExecuteReader();
                }

            }

           
        }
    }
