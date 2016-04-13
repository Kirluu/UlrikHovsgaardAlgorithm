using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Svg;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Properties;

namespace UlrikHovsgaardWpf
{
    public static class GraphImageRetriever
    {
        private static string _accessString = "http://dcr.itu.dk:8023/trace/render";

        public static SvgDocument Retrieve(DcrGraph graph)
        {
            string result;
            string body = "src=" + graph.ToDcrFormatString();
            
            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                result = wc.UploadString("http://dcr.itu.dk:8023/trace/dcr", 
                                                    body);
            }

            Console.WriteLine(result);

            var svg = SvgDocument.FromSvg<SvgDocument>(result);
            
            svg.Draw().Save(@"D:\test.jpeg", ImageFormat.Jpeg);

            return svg;
        }
    }
}
