// YOU MUST MODIFY THIS FILE SEE THE NOTES IN THE FILE!
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebServer2023
{
    public class RequestHandler : RequestHandlerBase, IRequestHandler
    {
        /// You do need to implement what is in the IRequestHandler including Properties
        public METHOD Method { get; set; }
        public string Path { get; set; }  // Holds the Path e.g. "/index.html"
        public float HttpVersion { get; set; }  // Holds the version of https e.g. "1.0" or "1.1"
        public List<string> ReqHeaders { get; set; } = new List<string>();  // Holds a list of Request Headers: E.g. "Content-Type: text/html\r\n"
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string ResponsePage { get; set; }


        /// <summary>
        /// See notes in IRequestHandler and RequestHandlerBase
        /// </summary>
        /// <param name="request"></param>
        public RequestHandler(string request) : base(request)
        {
            var re = new Regex(Constants.REQUEST_LINE);
            var m = re.Match(request);
            if (m.Success)
            {
                SetMethod(m.Groups[1].Value);
                HttpVersion = float.Parse(m.Groups[3].Value);
                Path = m.Groups[2].Value;
                
                foreach (var s in request.Split("\r\n"))
                    if (s.Contains(':'))
                        ReqHeaders.Add($"{s}\r\n");
                
                if (Path.Any(char.IsControl))
                {
                    ProcessResponseCode(400);  // disallow any control chars in path.
                    return;
                }

                if (Path.Equals("/"))
                    Path = "/index.html";
                
                // assume that the resource exists. If not, the resource getter will change this.
                ProcessResponseCode(200);
                GetResource();
            }
            else 
                ProcessResponseCode(400);
        }

        void ProcessResponseCode(int code)
        {
            ResponseCode = code;
            switch (code)
            {
                case 200:
                    ResponseMessage = "Ok";
                    break;
                case 400:
                    ResponseMessage = "Bad Request";
                    Path = "/Error400.html";
                    break;
                case 404:
                    ResponseMessage = "Not Found";
                    Path = "/Error404.html";
                    break;
                case 500:
                    ResponseMessage = "Internal Server Error";
                    Path = "/Error500.html";
                    break;
            }
            GetResource();  // assume this runs since the paths should be correct.
        }

        /// <summary>
        /// See notes in IRequestHandler and RequestHandlerBase
        /// </summary>
        /// <param name="method"></param>
        public void SetMethod(string method)
        {
            // make sure logic is correct here
            Method = Enum.Parse<METHOD>(method);
        }

        /// <summary>
        /// See notes in IRequestHandler and RequestHandlerBase
        /// </summary>
        /// <returns></returns>
        public string GetResponse()
        {
            return $"HTTP/1.0 {ResponseCode} {ResponseMessage}\r\n" +
                   "\r\n" +
                   ResponsePage;
        }

        /// <summary>
        /// See notes in IRequestHandler and RequestHandlerBase
        /// </summary>
        /// <returns></returns>
        public bool GetResource()
        {
            try
            {
                ResponsePage = File.ReadAllText($"wwwroot{Path}");
                return true;
            }
            catch
            {
                ProcessResponseCode(404);
                return false;
            }
        }

        /// <summary>
        /// I used this for debugging. Just kind of nice to have around. If you
        /// are not familiar with this. It means you can call to stream on this
        /// object and log it to the Console easily. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Method = " + Method.ToString() + "\r\n");
            sb.Append("Path = " + Path + "\r\n");
            sb.Append("HTTP Version = " + HttpVersion + "\r\n");
            sb.Append("Request Headers = " + "\r\n");
            foreach (var s in ReqHeaders)
            {
                sb.Append("   " + s);
            }
            return sb.ToString();
        }
    }
}
