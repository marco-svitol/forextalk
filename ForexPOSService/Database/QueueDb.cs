using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace ForexPOSService
{
    public sealed class QueueDb : IDisposable
    {
        public IDbConnection Connection { get; set; }

        public QueueDb()
        {

            Connection = new SQLiteConnection(AppConfig.QueueDBConnString());
            if (!File.Exists(Connection.ConnectionString.Substring(12, Connection.ConnectionString.IndexOf(";") - 12)))
            {
                throw new SQLiteException(SQLiteErrorCode.CantOpen, $"Cannot open QueueDB with connection string: {AppConfig.QueueDBConnString()}");
            }
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
