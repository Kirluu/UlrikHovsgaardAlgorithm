using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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

            try
            {
                using (WebClient wc = new WebClient()) 
                {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    //var encodedBody = SharpVectors.HttpUtility.UrlEncode(body);
                    //if (encodedBody == null)
                    //{
                    //    return null;
                    //}
                    var encodedBody = body.Replace("&", "and"); // TODO: Replace all illegal characters...?
                    var result = await wc.UploadStringTaskAsync("http://dcr.itu.dk:8023/trace/dcr", encodedBody);

                    //TODO: don't save it as a file
                    System.IO.File.WriteAllText(tempFilePath, result);

                    
                    /*const int ScaleFactor = 2;
                    var svg = SvgDocument.FromSvg<SvgDocument>(result);
                    svg.Height *= ScaleFactor;
                    svg.Width *= ScaleFactor;
                    var bitmap = svg.Draw(); //.Save(path, ImageFormat.Jpeg);
                    
                    return svg;*/
                }


                //conversion options
                WpfDrawingSettings settings = new WpfDrawingSettings();
                settings.IncludeRuntime = true;
                settings.TextAsGeometry = true;

                FileSvgReader converter = new FileSvgReader(settings);

                var xamlFile = converter.Read(tempFilePath);


                return new DrawingImage(xamlFile);

            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/94456/load-a-wpf-bitmapimage-from-a-system-drawing-bitmap
        /// </summary>
        private static BitmapImage ToBitmapImage(Bitmap source)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                source.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
