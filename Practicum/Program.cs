using System;
using RecognitionCoreLibrary;

namespace Practicum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the full path of the directory:");
            string dirPath = Console.ReadLine();
            var recognitionResult = RecognitionCore.Recognise(dirPath);
            foreach (var image in recognitionResult)
            {
                var results = image.Value;
                foreach (var res in results)
                {
                    Console.WriteLine($"{image.Key}: {res.Label} found");
                }
            }
        }
    }
}
