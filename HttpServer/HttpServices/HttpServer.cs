using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer.HttpServices
{

    public abstract class HttpServer
    {

        public int port { get; private set; }
        private HttpListener hlistener { get; set; }

        protected Dictionary<string, KeyValuePair<object, HttpServerCallback>> ruleter = new Dictionary<string, KeyValuePair<object, HttpServerCallback>>();

        public HttpServer(int port)
        {
            this.port = port;
        }

        public void listen()
        {
            // System.Net.IPAddress.Any
            hlistener = new HttpListener();
            string[] prefixes = new string[] { "http://*:" + port + "/", "http://+:" + port + "/" };
            hlistener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            // 增加监听的前缀.
            foreach (string s in prefixes)
            {
                hlistener.Prefixes.Add(s);
            }
            hlistener.Start();

            Task task = Task.Factory.StartNew(() => {
                while (hlistener.IsListening)
                {
                    HttpListenerContext context = hlistener.GetContext();
                    Console.WriteLine("Http requesting...");

                    HttplistenTask processor = new HttplistenTask(context.Request, context.Response, this);
                    Thread thread = new Thread(new ThreadStart(processor.process));
                    thread.Start();

                    Console.WriteLine("Http responseting...");
                }
            });
            task.Wait();
        }

        public void GET(string path, HttpServerCallback callback)
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            ruleter.Add(path, new KeyValuePair<object, HttpServerCallback>("GET", callback));
        }

        public void POST(string path, HttpServerCallback callback)
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            ruleter.Add(path, new KeyValuePair<object, HttpServerCallback>("POST", callback));
        }

        public abstract void handleGETTask(HttplistenTask p);
        public abstract void handlePOSTTask(HttplistenTask p, StreamReader inputData);

        public delegate void HttpServerCallback(Dictionary<object, object> resp, Dictionary<string, object> reqdata);
    }

}
