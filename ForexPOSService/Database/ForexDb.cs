using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

namespace ForexPOSService
{
    public sealed class ForexDB : IDisposable
    {
        public IDbConnection Connection { get; set; }

        public ForexDB()
        {
            Connection = new MySqlConnection(AppConfig.ForexDBConnString());
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
