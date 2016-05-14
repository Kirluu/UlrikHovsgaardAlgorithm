using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardWpf.Data
{
    public static class GraphImageRetriever
    {
        public static async Task<DrawingImage> Retrieve(DcrGraph graph)
        {
            var body = "src=" + graph.ExportToXml();

            var tempFilePath = Path.Combine(Path.GetTempPath(), "SaveFile.svg");

            using (WebClient wc = new WebClient()) 
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                //var encodedBody = SharpVectors.HttpUtility.UrlEncode(body);
                //if (encodedBody == null)
                //{
                //    return null;
                //}
                    
                var encodedBody = Regex.Replace(body, @"[^\w\s<>/""=]", "");
                //var encodedBody = body.Replace(" & ", "and"); // TODO: Replace all illegal characters...?
                if (encodedBody.Contains("_")) encodedBody = encodedBody.Replace("_", "");

                var result = await wc.UploadStringTaskAsync("http://dcr.itu.dk:8023/trace/dcr", encodedBody);
                    
                //TODO: don't save it as a file
                System.IO.File.WriteAllText(tempFilePath, result);
                    
            }


            //conversion options
            WpfDrawingSettings settings = new WpfDrawingSettings();
            settings.IncludeRuntime = true;
            settings.TextAsGeometry = true;

            FileSvgReader converter = new FileSvgReader(settings);

            var xamlFile = converter.Read(tempFilePath);


            return new DrawingImage(xamlFile);
        }
        
    }
}
