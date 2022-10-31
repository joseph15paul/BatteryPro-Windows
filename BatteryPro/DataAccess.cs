using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyModel.Resolution;
using Windows.Storage;

namespace BatteryPro
{
    
    public static class DataAccess
    {
        private static bool isDone = false;
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
            isDone = true;
        }

        public static void AddData(string batteryLevel, bool status, string timeStamp)
        {
            string dbpath = Path.Combine(getDirectory(), "batteryPro.db");
            using (SqliteConnection db =
              new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO BatteryStats VALUES (NULL, @batteryLevel, @status, @timeStamp);";
                insertCommand.Parameters.AddWithValue("@batteryLevel", batteryLevel);
                insertCommand.Parameters.AddWithValue("@status", status);
                insertCommand.Parameters.AddWithValue("@timeStamp", timeStamp);

                insertCommand.ExecuteReader();
            }

        }

        private static string getDirectory()
        {
            string dir = "C:\\BatteryPro";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }
        public static ObservableCollection<BatteryStats> GetData()
        {
            ObservableCollection<BatteryStats> entries = new ObservableCollection<BatteryStats>();

            bool charging;
            string dbpath = Path.Combine(getDirectory(), "batteryPro.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                if (isDone)
                {
                    SqliteCommand selectCommand = new SqliteCommand
                        ("SELECT * from BatteryStats", db);

                    SqliteDataReader query = selectCommand.ExecuteReader();

                    while (query.Read())
                    {
                        BatteryStats batteryStats = new BatteryStats();
                        if (query.GetString(2)=="1")
                        {
                            charging = true;
                        }
                        else
                        {
                            charging = false;
                        }
                        batteryStats.batterylevel = (float)Convert.ToDouble(query.GetString(1));
                        batteryStats.isCharging = charging;
                        batteryStats.timeStamp = (query.GetString(3));
                        entries.Add(batteryStats);
                    }
                }
            }

            return entries;
        }
    }
}
