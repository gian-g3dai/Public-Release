using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnakinShared.Models;

namespace UnakinShared.Utils
{
    public class AppDatabase
    {
        public static SQLiteAsyncConnection database;

        #region "Initialize Database"        
        public AppDatabase()
        {
            initializeDB(GetDBFile());
            //_ = UpdateDatabase().ConfigureAwait(false);
        }
        public async void initializeDB(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            await database.EnableWriteAheadLoggingAsync();
        }
        public string GetDBFile()
        {
            string dbName = "AppSQLite.db3";
            string path = Unakin.UnakinPackage.Instance.UserDataPath;
            string dbPath = Path.Combine(path, dbName);
            // Check if your DB has already been extracted.
            if (!File.Exists(dbPath))
            {
                File.Copy(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Resources\AppSQLite.db3", dbPath);
            }
            return dbPath;
        }
        #endregion

        #region DBUpdate
        public const int LAST_DATABASE_VERSION = 3;
        public async Task UpdateDatabase()
        {
            int currentDbVersion = 0;
            currentDbVersion = await GetDatabaseVersion();

            if (currentDbVersion < LAST_DATABASE_VERSION)
            {
                int startUpgradingFrom = currentDbVersion + 1;
                switch (startUpgradingFrom)
                {
                    case 1: //starting version
                        goto case 2;
                    case 2:
                        await UpgradeFrom1To2();
                        goto case 3;
                    case 3:
                        await UpgradeFrom2To3();
                        goto case 4; 
                    case 4:
                        await UpgradeFrom3To4();
                        goto case 5;
                    case 5:
                    default:
                        break;
                }
                await SetDatabaseToVersion(LAST_DATABASE_VERSION);
                
            }
            await UpgradeFrom3To4();
        }
        private async Task<int> GetDatabaseVersion()
        {
            return await database.ExecuteScalarAsync<int>("PRAGMA user_version");
        }
        private async Task<int> SetDatabaseToVersion(int version)
        {
            return await database.ExecuteAsync("PRAGMA user_version =" + version.ToString());
        }
        internal async Task UpgradeFrom1To2()
        {
            await database.CreateTableAsync<ChatMaster>();
            await database.CreateTableAsync<ChatDetail>();
        }

        internal async Task UpgradeFrom2To3()
        {
            await database.CreateTableAsync<Agent>();
        }

        internal async Task UpgradeFrom3To4()
        {
            await database.CreateTableAsync<ChatMaster>();
            await database.CreateTableAsync<ChatDetail>();

            var qryDt = String.Concat("UPDATE ChatMaster SET ChatType = 1 WHERE ChatType IS NULL;");
            await AppDatabase.database.QueryAsync<ChatMaster>(qryDt);
        }
        #endregion

    }
}
