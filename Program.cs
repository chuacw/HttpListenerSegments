using System;
using System.Net;
using System.Threading;

namespace HttpListenerSegments
{
    class Program
    {
        private Thread threadListener;
        private HttpListener httpListener;
        private bool stopped;

        Program()
        {
            stopped = false;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:1234/");
            httpListener.Prefixes.Add("http://localhost:5678/");
            threadListener = new Thread(ListenerThread);
            threadListener.Start();
        }

        private void ReturnResponseWithInfo(HttpListenerResponse response, string message, int statusCode)
        {
            response.StatusCode = statusCode;
            var buf = System.Text.Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buf.Length;
            var output = response.OutputStream;
            output.Write(buf);
        }

        public void ListenerThread()
        {
            httpListener.Start();
            while (!stopped)
            {
                var context = httpListener.GetContext();
                ThreadPool.QueueUserWorkItem(delegate (object state) {
                    var lContext = state as HttpListenerContext;
                    var uri = lContext.Request.Url;
                    var len = uri.Segments.Length;
                    var responseHandled = false;
                    try
                    {
                        if (len >= 2)
                        {
                            var prefix = uri.Segments[1];
                            var prefixUrl = $"{uri.Scheme}://{uri.IdnHost}:{uri.Port}/";
                            if (prefix == "remove/")
                            {
                                ReturnResponseWithInfo(lContext.Response, "Prefix removed", 200);
                                lContext.Response.Close();
                                httpListener.Prefixes.Remove("http://localhost:5678/");
                                responseHandled = true;
                            }
                        }
                    }
                    catch
                    {
                        // Empty, just to catch any exceptions!
                    }
                    finally
                    {
                        if (!responseHandled)
                        {
                            ReturnResponseWithInfo(lContext.Response, "No handler found!", 200);
                        }
                    }
                }, context);
            }

        }

        static void Main(string[] args)
        {
            Program program = new Program();
            Console.Write("Ctrl Break to stop");
            Console.ReadLine();
        }

    }
}
