using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WatsonWebserver;

namespace ForexPOSService
{
    internal class WatsonAPIListener
    {
        public static Server APIServer = new Server(AppConfig.QueueListenerAddress(), AppConfig.QueueListenerPort(),false, DefaultRoute);
 
        public WatsonAPIListener()
        {
            // set default deny (deny all) with whitelist to permit specific IP addresses or networks
            //APIServer.AccessControl.Mode = AccessControlMode.DefaultDeny;
            //APIServer.AccessControl.Whitelist.Add("127.0.0.1", "255.255.255.255");
            // add static routes
            APIServer.StaticRoutes.Add(HttpMethod.PUT, "/queue/", InsertQueue);
            APIServer.StaticRoutes.Add(HttpMethod.PUT, "/apikey/", UpdateAPIKey);
            Log.Information($"Queue is listening at http://{AppConfig.QueueListenerAddress()}:{AppConfig.QueueListenerPort()}");
        }
        private static async Task InsertQueue(HttpContext ctx)
        {
            try
            {
                var bodyStr = "";
                using (StreamReader reader
                      = new StreamReader(ctx.Request.Data, Encoding.UTF8, true, 1024, true))
                {
                    bodyStr = reader.ReadToEnd();
                }

                //Convert json string to QueueModel class
                var json = JsonConvert.DeserializeObject<QueueModel>(bodyStr);
                Log.Logger.Debug($"APIListener | Received PUT: IDU={json.IDU} oid={json.Transaction.oid}");

                //add to DB
                if (Store.QueueInsert(json) == 1)
                {
                    ctx.Response.StatusCode = 201;
                    Log.Logger.Information($"APIListener | Added {json.IDU} oid {json.Transaction.oid} to queue");
                    await ctx.Response.Send("ok");
                }
                else
                {
                    ctx.Response.StatusCode = 500;
                    Log.Logger.Information($"APIListener | Failed adding {json.IDU} oid {json.Transaction.oid} to queue");
                    await ctx.Response.Send("fail");
                }
            }
            catch (Exception ex)
            {
                Log.Error("APIListener: InsertQueue error : " + ex.Message);
                await ctx.Response.Send("fail");
            }
        }

        private static async Task UpdateAPIKey(HttpContext ctx)
        {
            string res = "";
            //write in DB and send response to caller
            byte[] buffer = new byte[512];
            ctx.Request.Data.Read(buffer, 0, buffer.Length);

            //Convert json string to APIKeyModel class
            string jj = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var ApiKey = JsonConvert.DeserializeObject<ConfigModel>(jj);
            Log.Logger.Debug($"APIListener | Received PUT: APIKey={ApiKey.value}");

            try
            {
                Store.QueueSetAPIKey(ApiKey);
                ctx.Response.StatusCode = 201;
                Log.Logger.Information($"APIListener | APIKey updated with: {ApiKey.value}");
                res = "ok";
            }
            catch
            {
                ctx.Response.StatusCode = 500;
                Log.Logger.Error($"APIListener | APIKey update Failed!");
                res = "fail";
            }
            finally
            {
                await ctx.Response.Send(res);
            };
        }

        static async Task DefaultRoute(HttpContext ctx)
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send("Queue service is listening");
        }

    }
}

