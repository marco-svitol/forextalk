using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ForexPOSService
{
    static class ApiHelper
    {
        private static HttpClient ApiClient { get; set; }
        private static HttpClient QueueClient {get; set;}

        public static void InitializeClient()
        {
            ApiClient = new HttpClient
            {
                BaseAddress = new Uri(AppConfig.APIHelperBaseAddress())
            };
            Log.Information($"APIClient endpoint is : {ApiClient.BaseAddress}");
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //Verify identity and check token
            Log.Debug($"POSCode is : {Utils.POSId}");
            //Verify if ForexDB contains an APIToken
            ConfigModel ApiKey = null;
            try
            {
                ApiKey = Store.ForexGetAPIKey();
            }
            catch
            {
                Log.Error("APIHelper : ForexDB can't get APIKey from DB, maybe MySQL service is stopped?");
                //throw new Exception("APIHelper : ForexDB can't get APIKey from DB, maybe MySQL service is stopped?", ex);
            }
            if (ApiKey != null){
                Store.QueueSetAPIKey(ApiKey);
                Log.Debug("APIKey succesfully synched with Forex_DB");
            }
            ApiKey = Store.QueueGetAPIKey();
            Log.Debug("APIKey: " + Store.QueueGetAPIKey().value);
            if (ApiKey.value.Length == 0)
            {
                Log.Warning("APIKey is missing or empty. Please provide a valid APIKey to enable APIService message exchange with Dashboard.");
            }
        }

        public static void InitializeQueueClient()
        {
            QueueClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{AppConfig.QueueListenerAddress()}:{AppConfig.QueueListenerPort()}")
            };
            QueueClient.DefaultRequestHeaders.Accept.Clear();
            QueueClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<List<TransactionsAddReply>> TransactionsAdd(List<QueueModel> transactionlist)
        {
            Uri route = new Uri("/api/pos/transactionsAdd", UriKind.Relative);
            ApiAuth apiAuth = new ApiAuth { APIKey = Store.QueueGetAPIKey().value, POSCode = Utils.POSId };
            TransactionsAdd transactionsadd = new TransactionsAdd
            {
                apiAuth = apiAuth,
                transactions = transactionlist
            };

            var jsonString = JsonConvert.SerializeObject(transactionsadd);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            List<TransactionsAddReply> responseBody = null;
            using (HttpResponseMessage response = await ApiClient.PostAsync(route, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    responseBody = response.Content.ReadAsAsync<List<TransactionsAddReply>>().Result;
                }
                else
                {
                    var message = response.ReasonPhrase + ": " +  response.Content.ReadAsStringAsync().Result;
                    content.Dispose();
                    throw new HttpRequestException(message);
                }
            }
            content.Dispose();
            return responseBody;
        }

        public static async Task<List<ActionsGetReply>> ActionsGet()
        {
            Uri route = new Uri("/api/pos/ActionsGet", UriKind.Relative);
            ApiAuth ApiAuth = new ApiAuth { APIKey = Store.QueueGetAPIKey().value, POSCode = Utils.POSId };
            var jsonString = JsonConvert.SerializeObject(new {apiAuth = ApiAuth});
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            List<ActionsGetReply> responseBody = null;
            try
            {
                using (HttpResponseMessage response = await ApiClient.PostAsync(route, content).ConfigureAwait(true))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        responseBody = response.Content.ReadAsAsync<List<ActionsGetReply>>().Result;
                    }
                    else
                    {
                        var message = response.ReasonPhrase + ": " + response.Content.ReadAsStringAsync().Result;
                        content.Dispose();
                        throw new HttpRequestException(message);
                    }
                }
            }
            catch(HttpRequestException ex)
            {
                throw new HttpRequestException("",ex);
            }
            catch(Exception ex)
            {
                throw new Exception("Unhandled exception in ActionsGet", ex);
            }
            finally
            {
                if (content != null) content.Dispose();
            }
            return responseBody;
        }

        public static async Task<bool> ActionAck(ActionAck actionack)
        {
            Uri route = new Uri("/api/pos/ActionAck", UriKind.Relative);
            ApiAuth ApiAuth = new ApiAuth { APIKey = Store.QueueGetAPIKey().value, POSCode = Utils.POSId };
            var jsonString = JsonConvert.SerializeObject(new {apiAuth = ApiAuth, actionAck = actionack});
            //var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            //List<ApiModel.ActionsGetReply> responseBody = null;
            using (StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await ApiClient.PostAsync(route, content).ConfigureAwait(true))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var message = response.ReasonPhrase + ": " + response.Content.ReadAsStringAsync().Result;
                    throw new HttpRequestException(message);
                }
                //content.Dispose();
                return true;
            }
        }

        public static async Task<bool>QueueAdd(QueueModel queue)
        {
            Uri route = new Uri("/queue", UriKind.Relative);
            var jsonString = JsonConvert.SerializeObject(queue);
            using (StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await QueueClient.PutAsync(route, content).ConfigureAwait(true))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var message = response.ReasonPhrase + ": " +  response.Content.ReadAsStringAsync().Result;
                    throw new HttpRequestException(message);
                }
                return true;
            }   
        }
    }

}

