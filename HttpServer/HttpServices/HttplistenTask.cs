using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.HttpServices
{

    public class HttplistenTask
    {
        HttpListenerRequest req;
        HttpListenerResponse resp;
        public HttpServer srv;
        public Uri Url;

        public static string assetsPath = "htmls";

        public String http_method;

        private const int BUF_SIZE = 4096;

        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Mime Type conversion table
        /// </summary>
        private static IDictionary<string, string> _mimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"},

                #endregion
            };

        public HttplistenTask(HttpListenerRequest _req, HttpListenerResponse _resp, HttpServer _srv)
        {
            req = _req;
            resp = _resp;
            srv = _srv;
            Url = req.Url;
        }

        public void process()
        {
            http_method = req.HttpMethod;
            if (http_method.Equals("GET"))
            {
                handleGETTask();
                return;
            }
            else if (http_method.Equals("POST"))
            {
                handlePOSTTask();
                return;
            }
            else
            {
                doResponse(http_method);
            }

        }

        public void handleGETTask()
        {
            srv.handleGETTask(this);
        }

        public void handlePOSTTask()
        {
            Console.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (req.HasEntityBody)
            {
                content_len = (int)req.ContentLength64;
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    Console.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = req.InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    Console.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("get post data end");

            srv.handlePOSTTask(this, new StreamReader(ms));

        }

        public void doResponse(string respdata)
        {

            byte[] data = Encoding.UTF8.GetBytes(respdata);
            resp.AddHeader("Access-Control-Allow-Origin", "*");
            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            // Write out to the response stream (asynchronously), then close it
            resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        public void doRedirect(string url)
        {
            resp.Redirect(url);
        }

        public bool tryStaticAssetsResponse(string filename)
        {
            filename = filename.Substring(1);
            filename = Path.Combine(assetsPath, filename);

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    resp.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime)
                        ? mime
                        : "application/octet-stream";
                    resp.ContentLength64 = input.Length;
                    resp.AddHeader("Date", DateTime.Now.ToString("r"));
                    resp.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                    byte[] buffer = new byte[1024 * 32];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        resp.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();
                    resp.OutputStream.Flush();

                    resp.StatusCode = (int)HttpStatusCode.OK;
                    return false;
                }
                catch (Exception ex)
                {
                    resp.StatusCode = (int)HttpStatusCode.InternalServerError;

                    byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                    resp.OutputStream.Write(buffer, 0, buffer.Length);
                    resp.ContentType = "text/plain";
                }
            }
            return true;
        }
    }

}
