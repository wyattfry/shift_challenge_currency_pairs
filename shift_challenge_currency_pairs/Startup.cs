using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace shift_challenge_currency_pairs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseMvc();

            // 
            // this sockets implementation borrows much from
            // https://blog.xamarin.com/developing-real-time-communication-apps-with-websocket/
            //
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await HandlePairLookupRequest(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        private async Task HandlePairLookupRequest(HttpContext context, WebSocket webSocket)
        {

            Dictionary<string, double> pairs = new Dictionary<string, double>();
            pairs.Add("BTCUSD", 6140.9951);
            pairs.Add("ETHUSD", 473.995);
            pairs.Add("ETHBTC", 0.077060);
            pairs.Add("XRPUSD", 0.48723);

            WebSocketReceiveResult result;
            var message = new ArraySegment<byte>(new byte[4096]);
            string receivedMessage = "";
            do
            {
                result = await webSocket.ReceiveAsync(message, CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    break;
                }
                var messageBytes = message.Take<byte>(result.Count).ToArray<byte>();
                receivedMessage = Encoding.UTF8.GetString(messageBytes);
                var byteMessage = Encoding.UTF8.GetBytes("(no matching pair found)"); ;
                if (pairs.ContainsKey(receivedMessage))
                {
                    double exchangeRate = pairs[receivedMessage];
                    byteMessage = Encoding.UTF8.GetBytes(exchangeRate.ToString());
                }
                var segment = new ArraySegment<byte>(byteMessage);
                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            while (!result.CloseStatus.HasValue);
        }
    }
}
