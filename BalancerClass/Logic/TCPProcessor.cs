using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BalancerClass.Logic
{
    public class TCPProcessor
    {

        private static List<string> apiServers = new List<string> { "http://api1:5000", 
            "http://api2:5000", "http://api3:5000" };
        private static int currentIndex = 0;
        private static object lockObject = new object();

        HttpListener listener;
        public void ListenForTraffic()
        {
            try
            {
                listener = new HttpListener();

                listener.Prefixes.Add("");


                listener.Start();
                Console.WriteLine("************************************************");
                Console.WriteLine("Listening For an HTTP Request...");
                Console.WriteLine("************************************************");


                while (true)
                {
                    try
                    {

                        HttpListenerContext context = listener.GetContext();
                        Thread workerThread = new Thread(new ParameterizedThreadStart(HandleRequest));
                        workerThread.Start(context);


                        //ThreadPool.QueueUserWorkItem(HandleRequest, context);
                        //HandleRequest(context);
                    }
                    catch (Exception ex)
                    {
                        //log Errors into a file;
                        Console.WriteLine(ex.Message);

                        File.AppendAllText(@"D:\CollectionErrorLogs.txt", ex.Message + DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void HandleRequest(object httpContext)
        {
            HttpListenerContext context = (HttpListenerContext)httpContext;

            string targetApi;

            lock (lockObject)
            {
                targetApi = apiServers[currentIndex];

                currentIndex = (currentIndex + 1) % apiServers.Count;
            }

            Console.WriteLine($"Redirecting request to: {targetApi}");

            try
            {
                // Read incoming JSON payload
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string requestBody = reader.ReadToEnd();

                    byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);

                    // Forward the JSON payload to the target API
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(targetApi + context.Request.RawUrl);

                    request.Method = context.Request.HttpMethod;
                    request.ContentType = context.Request.ContentType;
                    request.ContentLength = requestBodyBytes.Length;

                    System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
                    // Set TLS 1.2
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    ServicePointManager.DefaultConnectionLimit = 1000;


                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(requestBodyBytes, 0, requestBodyBytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())

                    using (var responseStream = response.GetResponseStream())
                    {

                        var responseString = new StreamReader(responseStream).ReadToEnd();

                        string jsonresp = responseString;

                        //return the response
                        byte[] buf = Encoding.ASCII.GetBytes(jsonresp);

                        context.Response.ContentLength64 = buf.Length;

                        context.Response.OutputStream.Write(buf, 0, buf.Length);
                        //context.Response.ContentType = response.ContentType;

                        //context.Response.ContentLength64 = response.ContentLength;

                        //responseStream.CopyTo(context.Response.OutputStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                context.Response.Close();
            }
        }

        private bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
