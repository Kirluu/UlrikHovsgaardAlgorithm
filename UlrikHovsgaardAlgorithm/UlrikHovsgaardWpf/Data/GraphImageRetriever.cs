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
using Services;

namespace UlrikHovsgaardWpf.Data
{
    public static class GraphImageRetriever
    {
        public static DrawingImage RetrieveLocal(DcrGraph graph)
        {
            var body = "src=" + graph.ExportToXml();
            var encodedBody = Regex.Replace(body, @"[^\w\s<>/""=]", "");
            //var encodedBody = body.Replace(" & ", "and"); // TODO: Replace all illegal characters...?
            if (encodedBody.Contains("_")) encodedBody = encodedBody.Replace("_", "");


            //conversion options
            WpfDrawingSettings settings = new WpfDrawingSettings();
            settings.IncludeRuntime = true;
            settings.TextAsGeometry = true;

            FileSvgReader converter = new FileSvgReader(settings);

            var x = graph.ExportToXml();

            //var xamlFile = converter.Read(Services.Handlers.net(x));
            //var xamlFile = converter.Read(Services.Handlers.net(encodedBody));
            //var xamlFile = converter.Read(Services.Handlers.net(body));
            var xamlFile = converter.Read(Services.Handlers.render(x));
            //var xamlFile = converter.Read(Services.Handlers.render(body));
            //var xamlFile = converter.Read(Services.Handlers.render(encodedBody));
            

            return new DrawingImage(xamlFile);
        }

        public static async Task<DrawingImage> Retrieve(DcrGraph graph)
        {
            var body = "src=" + graph.ExportToXml();
            var encodedBody = Regex.Replace(body, @"[^\w\s<>/""=]", "");
            //var encodedBody = body.Replace(" & ", "and"); // TODO: Replace all illegal characters...?
            if (encodedBody.Contains("_")) encodedBody = encodedBody.Replace("_", "");


            var tempFilePath = Path.Combine(Path.GetTempPath(), "SaveFile.svg");

            using (WebClient wc = new WebClient()) 
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                //var encodedBody = SharpVectors.HttpUtility.UrlEncode(body);
                //if (encodedBody == null)
                //{
                //    return null;
                //}
                    
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
