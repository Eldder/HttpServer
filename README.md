# HttpServer
C# Http Server Demo baseed on [System.Net] -> [HttpListener]

### demo to use
~~~
            DemoHttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new DemoHttpServer(Convert.ToInt16(args[0]));
            }
            else
            {
                httpServer = new DemoHttpServer(8080);
            }
            httpServer.GET("get.do", (p, data) =>
            {
                p.Add("data", "1");
            });
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();
            System.Diagnostics.Process.Start("http://localhost:" + httpServer.port + "/index.html");
~~~

### (p, data) 

p -> [Dictionary<object, object> resp] : the reponse data set

data -> [Dictionary<string, object> reqdata] : the request data set

demo post
~~~
            httpServer.POST("post.do", (p, data) =>
            {

                string param = "";

                if (data.ContainsKey("param"))
                {
                    param = data["param"].ToString();
                }
                p.Add("param", param);
            });
~~~


