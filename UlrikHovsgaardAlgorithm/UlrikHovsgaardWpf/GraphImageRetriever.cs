using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using Svg;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Properties;

namespace UlrikHovsgaardWpf
{
    public static class GraphImageRetriever
    {
        public static async Task<BitmapImage> Retrieve(DcrGraph graph)
        {
            var body = "src=" + graph.ToDcrFormatString();
            
            try
            {
                using (WebClient wc = new WebClient()) // TODO: Find out cause of bug where subsequent calls returns "" string...
                {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    var result = await wc.UploadStringTaskAsync("http://dcr.itu.dk:8023/trace/dcr", body);
                    //Console.WriteLine(result);

                    var svg = SvgDocument.FromSvg<SvgDocument>(result);
                    var bitmap = svg.Draw(); //.Save(path, ImageFormat.Jpeg);

                    return ToBitmapImage(bitmap);
                }
            }
            catch
            {
                return null;
            }
        }
            
        private static BitmapImage ToBitmapImage(Bitmap source)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                source.Save(memory, ImageFormat.Bmp);
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
