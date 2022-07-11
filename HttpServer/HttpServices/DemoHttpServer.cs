using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace HttpServer.HttpServices
{
    class DemoHttpServer : HttpServer
    {

        public DemoHttpServer(int port)
            : base(port)
        {

        }

        public static Dictionary<string, object> ParseQueryString(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("url");
            }
            var uri = new Uri("http://localhost" + url);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                return new Dictionary<string, object>();
            }
            //1.去除第一个前导?字符
            var dic = uri.Query.Substring(1)
                    //2.通过&划分各个参数
                    .Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    //3.通过=划分参数key和value,且保证只分割第一个=字符
                    .Select(param => param.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries))
                    //4.通过相同的参数key进行分组
                    .GroupBy(part => part[0], part => part.Length > 1 ? part[1] : string.Empty)
                    //5.将相同key的value以,拼接
                    .ToDictionary(group => group.Key, group => string.Join(",", group));
            Dictionary<string, object> res = new Dictionary<string, object>();
            foreach (string key in dic.Keys)
            {
                res.Add(key, dic[key]);
            }
            return res;
        }

        public override void handleGETTask(HttplistenTask p)
        {

            foreach (string path in ruleter.Keys)
            {

                if (p.Url.AbsolutePath.Equals(path) && "GET".Equals(ruleter[path].Key))
                {
                    Dictionary<string, object> dic = ParseQueryString(p.Url.PathAndQuery);
                    Console.WriteLine("GET request: {0} {1}", p.Url.PathAndQuery, JsonConvert.SerializeObject(dic));
                    System.Console.WriteLine("{0} - {1}", p.http_method, p.Url.PathAndQuery);
                    Dictionary<object, object> resp = new Dictionary<object, object>();
                    ruleter[path].Value(resp, dic);
                    //p.httpHeaders.Add("Access-Control-Allow-Origin", "*");
                    //p.httpHeaders.Add("Content-Type", "application/json");
                    //p.writeSuccess();
                    //p.outputStream.WriteLine();

                    p.doResponse(JsonConvert.SerializeObject(resp));
                    return;
                }
                if (p.Url.AbsolutePath.Equals(path) && "NEXT".Equals(ruleter[path].Key))
                {
                    Dictionary<object, object> resp = new Dictionary<object, object>();
                    ruleter[path].Value(resp, new Dictionary<string, object>());
                    p.doRedirect(resp.Keys.First().ToString());
                }

            }
                if (p.tryStaticAssetsResponse(p.Url.AbsolutePath))
            {
                Console.WriteLine("request: {0}", p.Url.OriginalString);
                ConsoleColor coler = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                string error = " {\"error\":\"undefined request path: " + p.Url.AbsolutePath + "\"}";
                Console.ForegroundColor = coler;
                p.doResponse(error);
            }

        }

        public override void handlePOSTTask(HttplistenTask p, StreamReader inputData)
        {

            foreach (string path in ruleter.Keys)
            {

                if (p.Url.AbsolutePath.Equals(path) && "POST".Equals(ruleter[path].Key))
                {
                    // Dictionary<string, string> dic = ParseQueryString(p.Url.PathAndQuery);
                    string reponsedata = inputData.ReadToEnd();
                    Dictionary<string, object> dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(reponsedata);
                    // Console.WriteLine("POST request: {0} {1}", p.Url.PathAndQuery, JsonConvert.SerializeObject(dic));
                    // System.Console.WriteLine("{0} - {1}", p.http_method, p.Url.PathAndQuery);


                    Dictionary<object, object> resp = new Dictionary<object, object>();
                    ruleter[path].Value(resp, dic);
                    //p.httpHeaders.Add("Access-Control-Allow-Origin", "*");
                    //p.httpHeaders.Add("Content-Type", "application/json");
                    //p.writeSuccess();
                    p.doResponse(JsonConvert.SerializeObject(resp));
                    return;
                }

            }
            Console.WriteLine("request: {0}", p.Url.PathAndQuery);
            p.doResponse(JsonConvert.SerializeObject("{\"error\":\"接口不存在\"}"));
        }
    }
}