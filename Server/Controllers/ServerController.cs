using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using YOLOv4MLNet.DataStructures;
using RecognitionCoreLibrary;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Collections.Immutable;
using DatabaseManager;
using Contract;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    [ApiController]
    public class ServerController : ControllerBase
    {
        private ImageStoreContext db;
        private CancellationTokenSource source;

        public ServerController(ImageStoreContext dB, CancellationTokenSource Source)
        {
            this.db = dB;
            this.source = Source;
        }
        private byte[] ImageToByteArray(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
        [Route("detect")]
        public async Task<ImmutableList<RequestImage>> Get(string path)
        {
            var recognitionResult = new ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>>();
            var images = ImmutableList.Create<RequestImage>();

            var task1 = Task.Factory.StartNew(() => RecognitionCore.Recognise(path, recognitionResult, source.Token), TaskCreationOptions.LongRunning);
            var task2 = Task.Factory.StartNew(() =>
            {
                while (task1.Status == TaskStatus.Running)
                {
                    while (recognitionResult.TryDequeue(out Tuple<string, IReadOnlyList<YoloV4Result>> result))
                    {
                        string name = result.Item1;
                        var bitmap = new Bitmap(Image.FromFile(Path.Combine(path, name)));
                        using var g = Graphics.FromImage(bitmap);
                        // Create ProcessedImage object
                        var currImage = new ProcessedImage
                        {
                            Objects = new List<RecognizedObject>()
                        };
                        foreach (var res in result.Item2)
                        {
                            var x1 = res.BBox[0];
                            var y1 = res.BBox[1];
                            var x2 = res.BBox[2];
                            var y2 = res.BBox[3];
                            g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                            using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                            {
                                g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                            }

                            g.DrawString(res.Label, new Font("Arial", 12),
                                         Brushes.Blue, new PointF(x1, y1));
                            // Add recognized object to currImage
                            currImage.Objects.Add(new RecognizedObject() { ClassName = res.Label, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
                        }
                        // Fill BLOB in currImage
                        currImage.ImageContent = ImageToByteArray(bitmap);
                        // Add currImage to DB
                        db.AddImage(currImage);

                        var image = new RequestImage();
                        image.ImageClass = name;
                        image.Bitmap = ImageToByteArray(bitmap);
                        images = images.Add(image);
                    }
                }
            }, TaskCreationOptions.LongRunning);
            await task2;
            return images;
        }

        [Route("get-images")]
        public List<ProcessedImage> Get()
        {
            return db.Images.ToList();
        }

        [Route("clear")]
        public void Delete()
        {
            var images = db.Images.Include(e => e.Objects);
            db.RemoveRange(images);
            db.SaveChanges();
        }

        [Route("stop")]
        public void Stop()
        {
            source.Cancel();
        }
    }
}
