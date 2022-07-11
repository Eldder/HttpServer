using HttpServer.HttpServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            DemoHttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new DemoHttpServer(Convert.ToInt16(args[0]));
            }
            else
            {
                httpServer = new DemoHttpServer(8080);
            }
            httpServer.GET("/", (p, data) =>
            {
                p.Add("data", "1");
            });
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();
            System.Diagnostics.Process.Start("http://localhost:" + httpServer.port + "/index.html");
        }
    }
}
