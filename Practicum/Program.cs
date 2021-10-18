using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecognitionCoreLibrary;
using YOLOv4MLNet.DataStructures;

namespace Practicum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the full path of the directory:");
            string dirPath = Console.ReadLine();
            var recognitionResult = new ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>>();

            var source = new CancellationTokenSource();
            var token =source.Token;

            Console.WriteLine("Print c or C to stop execution");
            var cancelTack = Task.Factory.StartNew(() =>
            {
                char ch = Console.ReadKey().KeyChar;
                if (ch == 'c' || ch == 'C')
                {
                    source.Cancel();
                    Console.WriteLine("\nTask cancellation requested");
                }

            }, TaskCreationOptions.LongRunning);
            var task1 = Task.Factory.StartNew(() => RecognitionCore.Recognise(dirPath, recognitionResult, token), TaskCreationOptions.LongRunning);
            var task2 = Task.Factory.StartNew(() =>
            {
                while (task1.Status == TaskStatus.Running)
                {
                    while(recognitionResult.TryDequeue(out Tuple<string, IReadOnlyList<YoloV4Result>> result))
                    {
                        string name = result.Item1;
                        foreach(var res in result.Item2)
                        {
                            var x1 = res.BBox[0];
                            var y1 = res.BBox[1];
                            var x2 = res.BBox[2];
                            var y2 = res.BBox[3];
                            Console.WriteLine($"In image {name} {res.Label} was found in a ractagle between ({x1:0.0}, {y1:0.0}) and ({x2:0.0}, {y2:0.0}) coordinates");
                        }
                    }    
                }
            });
            Task.WaitAll(task1, task2);
        }
    }
}
