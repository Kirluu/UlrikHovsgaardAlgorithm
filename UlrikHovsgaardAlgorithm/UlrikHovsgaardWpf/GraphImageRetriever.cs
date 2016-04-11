using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardWpf
{
    public static class GraphImageRetriever
    {
        private static string _accessString = "http://dcr.itu.dk:8023/trace/render";

        public static Image Retrieve(DcrGraph graph)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_accessString);
            request.Method = "POST";
            request.AllowAutoRedirect = false;
            //request.CookieContainer = my_cookie_container;
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"; // TODO: wut
            request.Accept = "svg+xml";
            request.ContentType = "application/x-www-form-urlencoded";

            string strNew = "source=" + HttpUtility.UrlEncode(graph.ExportToXml());

            using (StreamWriter stOut = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII))
            {
                stOut.Write(strNew);
                stOut.Close();
            }

            WebResponse response = request.GetResponse();
            var lala = response.ContentLength;



            return null;
        }
    }
}
