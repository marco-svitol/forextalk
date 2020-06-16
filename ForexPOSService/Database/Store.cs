using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using System.Data;


namespace ForexPOSService
{
    public static class Store
    {
        private static readonly QueueDb queuedb = new QueueDb();
        private static readonly ForexDB forexdb = new ForexDB();
        private static readonly IDbConnection qConnection = queuedb.Connection;
        private static readonly IDbConnection fConnection = forexdb.Connection;

        public static void StoreInitialize(){
            qConnection.Open();
        }

        public static List<QueueModel>QueueGet()
        {
            string query = "" +
                "SELECT " +
                "QId," +
                "IDU," +
                "oid," +
                "account_year," +
                "forex_type," +
                "forex_oid," +
                "foreign_amount," +
                "exchange_rate," +
                "national_amount," +
                "journal_date_time," +
                "IFNULL(userID,'n/a')," +
                "isPOStransaction " +
                "FROM Queue ORDER BY Oid LIMIT 10";
            //var result = qConnection.Query<QueueModel>(query, new DynamicParameters()).ToList();
            var queue = new List<QueueModel>();
            using (var reader = qConnection.ExecuteReader(query))
            {
                //ForexTransactionModel transaction = null;
                while (reader.Read())
                {
                    ForexTransactionModel transaction = new ForexTransactionModel
                    {
                        oid = reader.GetInt32(2),
                        account_year = reader.GetInt32(3),
                        forex_type = reader.GetInt32(4),
                        forex_oid = reader.GetInt32(5),
                        foreign_amount = reader.GetDecimal(6),
                        exchange_rate = reader.GetDouble(7),
                        national_amount = reader.GetDecimal(8),
                        journal_date_time = reader.GetDateTime(9) ,
                        userID = reader.GetString(10),
                        isPOStransaction = reader.GetInt32(11),
                    };

                    queue.Add(new QueueModel
                    {
                        QId = reader.GetInt32(0),
                        IDU = reader.GetString(1),
                        Transaction = transaction
                    });
                }
            }

            return queue;
        }

        public static int QueueInsert(QueueModel queueitem)
        {
            if (queueitem != null)
            {
                var result = qConnection.Execute("" +
                    "INSERT INTO Queue(" +
                    "IDU, " +
                    " oid, account_year, forex_type, forex_oid, foreign_amount, exchange_rate, national_amount, journal_date_time, userID, isPOStransaction" +
                    ") SELECT " +
                    "@IDU, " +
                    "@oid, " +
                    "@account_year, " +
                    "@forex_type, " +
                    "@forex_oid, " +
                    "@foreign_amount, " +
                    "@exchange_rate, " +
                    "@national_amount, " +
                    "@journal_date_time, " +
                    "IFNULL(@userId,'n/a'), " +
                    "@isPOStransaction " +
                    " WHERE NOT EXISTS(SELECT 1 FROM Queue WHERE oid = @oid AND IDU=@IDU)"
                    , new
                    {
                        queueitem.IDU,
                        queueitem.Transaction.oid,
                        queueitem.Transaction.account_year,
                        queueitem.Transaction.forex_type,
                        queueitem.Transaction.forex_oid,
                        queueitem.Transaction.foreign_amount,
                        queueitem.Transaction.exchange_rate,
                        queueitem.Transaction.national_amount,
                        queueitem.Transaction.journal_date_time,
                        queueitem.Transaction.userID,
                        queueitem.Transaction.isPOStransaction
                    });
                return result;
            }
            else return 0;
        }

        public static int QueuePull(List<TransactionsAddReply> pulllist) //oltre all'oid anche il tipo oppure qid!!
        {
            if (pulllist != null)
            {
                //qConnection.Open();
                int result = 0;
                var added = pulllist.FindAll(x => x.added == true).Select(x => x.QId).ToArray();
                var notadded = pulllist.FindAll(x => x.added == false).Select(x => x.QId).ToArray();
                if (notadded.Length > 0)
                {
                    result += qConnection.Execute("" +
                     "INSERT INTO FailedQueue SELECT * FROM Queue WHERE QId in @_notaddedlist", new { _notaddedlist = notadded });
                }
                result += qConnection.Execute("" +
                    "DELETE FROM Queue WHERE QId in @_addedlist OR QId in @_notaddedlist", new { _addedlist = added, _notaddedlist = notadded });
                //qConnection.Close();
                return result;
            }
            else return 0;
        }

        public static ConfigModel QueueGetAPIKey()
        {
            ConfigModel apikey = null;
            //qConnection.Open();
            var result = qConnection.Query<ConfigModel>("" +
                "SELECT * FROM config WHERE param = 'ApiKey'");
            //qConnection.Close();
            if (result.Any())
            {
                apikey = result.First();
                return apikey;
            }
            //APIKey = new ConfigModel { param = "ApiKey", value = "" };
            return apikey;
        }

        public static int QueueSetAPIKey(ConfigModel apikey)
        {
            if (apikey != null)
            {
                //qConnection.Open();
                var result = qConnection.Execute("" +
                    "INSERT INTO config(param, value) VALUES('ApiKey', @ApiKey) ON CONFLICT(param) DO UPDATE SET 'value' = @ApiKey", new { ApiKey = apikey.value });
                //qConnection.Close();
                return result;
            }
            else return 0;
        }

        public static List<ForexTransactionModel> ForexGetTransaction(int[] oids)
        {
            {
                fConnection.Open();
                {
                    using (var cmd = fConnection.CreateCommand())
                    {
                        var result = fConnection.Query<ForexTransactionModel>("SELECT * FROM forex_transaction WHERE oid in @_oids;", new { _oids = oids });
                        fConnection.Close();
                        return result.ToList();
                    }
                }
            }
        }

        public static ForexTransactionModel ForexWriteTransaction(ForexTransactionModel transaction)
        {
            if (transaction != null) {
                fConnection.Open();
                var result = fConnection.ExecuteScalar<int>("INSERT INTO forex_transaction " +
                   "( account_year, forex_type, forex_oid, foreign_amount, exchange_rate, national_amount, journal_date_time, userID, isPOStransaction ) VALUES(" +
                    "@account_year," +
                    "@forex_type, " +
                    "@forex_oid, " +
                    "@foreign_amount, " +
                    "@exchange_rate, " +
                    "@national_amount, " +
                    "@journal_date_time, " +
                    "@UserID, " +
                    "@isPOStransaction); select LAST_INSERT_ID(); ", transaction);
                fConnection.Close();
                if (result <= 0) {
                    throw new Exception("Nothing was written on ForexDB");
                }
                transaction.oid = result;
                return transaction;
            } else return null;
        }

        public static string ForexWriteAccountData(ForexTransactionModel transaction)
        {
            fConnection.Open();
            using (var cmd = fConnection.CreateCommand())
            {
                var result = fConnection
                    .Query<ForexTransactionModel>("INSERT INTO forex_transaction(" +
                    " ", new { _transaction = transaction });
                fConnection.Close();
                return result.ToString();
            }
        }

        public static ConfigModel ForexGetAPIKey()
        {
            fConnection.Open();
            var result = fConnection.Query<ConfigModel>("SELECT 'ApiKey' as param, value FROM forex_config WHERE parameter = 'APIKey';");
            fConnection.Close();
            return result.First();
        }    
    }
}
