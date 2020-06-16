using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Timers;

namespace ForexPOSService
{
    public class MainService
    {
        private readonly Timer _pullConsoleAction;
        private readonly Timer _FetchForexTransactionsInterval;
        public MainService()
        {
            _pullConsoleAction = new Timer(AppConfig.PullConsoleActionInterval()) { AutoReset = true };
            _pullConsoleAction.Elapsed += new ElapsedEventHandler(PullConsoleAction);
            _pullConsoleAction.Enabled = true;
            _FetchForexTransactionsInterval = new Timer(AppConfig.FetchForexTransactionsInterval()) { AutoReset = true };
            _FetchForexTransactionsInterval.Elapsed += new ElapsedEventHandler(SyncForexTransactions);
            _FetchForexTransactionsInterval.Enabled = true;
        }

        private async void PullConsoleAction(object sender, ElapsedEventArgs e)
        {
            Log.Verbose("PullConsoleAction: scheduled start");
            try 
	        {	        
		        //Pull actions from remote ConsoleAPI 
                var responseBody = await ApiHelper.ActionsGet();
                var logmsg = $"PullConsoleAction: fetched {responseBody.Count} actions from console";
                if (responseBody.Count > 0) Log.Information(logmsg); else Log.Debug(logmsg);
                //iterate through actions
                foreach (ActionsGetReply res in responseBody){
                    //parse params
                    Log.Debug($"PullConsoleAction: processing action {res.POSActionQueueId}");
                    ActionSendToPosParams resparams = JsonConvert.DeserializeObject<ActionSendToPosParams>(res.POSActionParams);
                    Log.Verbose($"PullConsoleAction: processing action {res.action} {res.POSActionQueueId}.Params: amount={resparams.amount},curr={resparams.currency},exch={resparams.exchangerate}");
                    ForexTransactionModel transaction = null;
                    switch (res.action){
                        case "sendtopos":
                            //write on ForexDB
                            transaction = new ForexTransactionModel{
                                account_year = DateTime.Now.Year,
                                forex_type = 3, //Deposit
                                forex_oid = resparams.currency,
                                foreign_amount = resparams.amount,
                                exchange_rate = resparams.exchangerate,
                                national_amount = 0,
                                journal_date_time = DateTime.Now,
                                userID = "console",
                                isPOStransaction = 0
                            };
                            break;
                        case "CHFtransfer":
                              transaction = new ForexTransactionModel{
                                account_year = DateTime.Now.Year,
                                forex_type = 2, //National currency IN
                                forex_oid = 1, //always CHF for POS transactions
                                foreign_amount = 0, //always 0
                                exchange_rate = 0, //always 0
                                national_amount = -resparams.amount, // negative amount is a workaround ! ;) 
                                journal_date_time = DateTime.Now,
                                userID = "console",
                                isPOStransaction = 1
                            };
                            break;
                        default:
                            Log.Error($"PullConsoleAction: action {res.action} is unknown");
                            break;
                    }
                    if (transaction == null) continue;
                    //Write transaction on ForexDB and then on local Queue to Sync remote DB.
                    //No need to check reply, this call raises an error in case http request fails
                    await ApiHelper.QueueAdd(new QueueModel{IDU = "I", Transaction = Store.ForexWriteTransaction(transaction)});
                    Log.Debug($"PullConsoleAction: Desposit transaction inserted in ForexDB and added to Queue");
                    //Feedback to console to remove Actions from remote queue
                    await ApiHelper.ActionAck(new ActionAck{POSActionQueueId = res.POSActionQueueId});
                    Log.Debug($"PullConsoleAction: action {res.POSActionQueueId} acknowledged to console");
                }
	        }
	        catch (HttpRequestException ex)
            {
                var inner = ex.InnerException;
                string StackMessage = "Error fetching actions from console: Api request error: " + ex.Message;
                while (inner != null)
                {
                    StackMessage += ": " + inner.Message;
                    inner = inner.InnerException;
                }
                Log.Error(StackMessage);
            }
            catch (MySqlException ex)
            {
                string errmsg = "Error fetching actions from console: ForexDB access error. ";
                if (!Utils.MySQLIsRunning())
                {
                    errmsg += " MySQL service is not running, cannot write data to local Forex DB.";
                }
                else
                {
                    errmsg += ex.Message + ex.StackTrace;
                }
                Log.Error(errmsg);
            }
            catch (Exception ex) {
                var inner = ex.InnerException;
                string stacktrace = "Error fetching actions from console: " + ex.Message + ex.StackTrace;
                while (inner != null)
                {
                    stacktrace += inner.StackTrace;
                    inner = inner.InnerException;
                }
                Log.Error(stacktrace);
            }
        }

        private async void SyncForexTransactions(object sender, ElapsedEventArgs e)
        {
            Log.Verbose("SyncForexTransactions: scheduled start");
            try
            {
                //Fetch Queue from QueueDB
                List<QueueModel> queueList = Store.QueueGet();
                if (queueList.Count == 0) {
                    Log.Debug("SyncForexTransactions: empty queue");
                    return;
                }
                Log.Debug($"SyncForexTransactions:fetched {queueList.Count} item from queue");
                var responseBody = await ApiHelper.TransactionsAdd(queueList);
                var added = responseBody.FindAll(x => x.added == true);
                var notadded = responseBody.FindAll(x => x.added == false);
                var logmsg = $"SyncForexTransactions: console replied with {added.Count} added transactions and {notadded.Count} not added.";
                if (notadded.Count > 0) Log.Warning(logmsg); else Log.Information(logmsg);
                //pull oids from queue and save Failed ones
                if (Store.QueuePull(responseBody) == 0)
                {
                    throw new Exception($"Pulling {added.Count+ notadded.Count} oids from queue failed");
                }
                else
                {
                    Log.Debug($"SyncForexTransactions: transactions succesfully pulled from queue");
                }
            }
            catch (HttpRequestException ex)
            {
                var inner = ex.InnerException;
                string StackMessage = "Error fetching actions from console: Api request error: " + ex.Message;
                while (inner != null)
                {
                    StackMessage += ": " + inner.Message;
                    inner = inner.InnerException;
                }
                Log.Error(StackMessage);
            }
            catch (Exception ex) {
                var inner = ex.InnerException;
                string stacktrace = "Error while syncing queue: " + ex.Message + ex.StackTrace;
                while (inner != null)
                {
                    stacktrace += inner.StackTrace;
                    inner = inner.InnerException;
                }
                Log.Error(stacktrace);
            }
        }

        public void Start()
        {
            _pullConsoleAction.Start();
            _FetchForexTransactionsInterval.Start();
            LogEventLevel currlel = 0;
            foreach (LogEventLevel lel in Enum.GetValues(typeof(LogEventLevel)))
            {
                if (Log.IsEnabled(lel)) { currlel = lel; break; }
            }
            Log.Information($"Log minimum level is set to: {currlel.ToString()} ");
        }

        public void Stop()
        {
            _pullConsoleAction.Stop();
            _FetchForexTransactionsInterval.Stop();
            Log.CloseAndFlush();
        }
    }
}
