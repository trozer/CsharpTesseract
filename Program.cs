using CsharpTesseract4.Interop;
using InteropDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace CsharpTesseract4
{
    class Program
    {
        static void Main(string[] args)
        {
            TesseractEngine engine = new TesseractEngine("F:\\Work\\Tesseract\\CsharpTesseract\\CsharpTesseract4\\CsharpTesseract4\\CsharpTesseract4\\bin\\x64\\Debug\\tessdata", "hun",
                EngineMode.LSTMOnly, new string[0], new Dictionary<string, object>(), false);

            Bitmap kutya = new Bitmap(@"F:\\Work\\Tesseract\\CsharpTesseract\\_leves.bmp");
            //var testImagePath = "F:\\Work\\Tesseract\\CsharpTesseract\\_leves.bmp";
            //var img = Pix.LoadFromFile(testImagePath);
            var watch = System.Diagnostics.Stopwatch.StartNew();

            BitmapToPixConverter converter = new BitmapToPixConverter();
            var img = converter.Convert(kutya);

            
            var page = engine.Process(img);

            var text = page.GetText();
            Console.WriteLine(text);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            
            

            Console.WriteLine(elapsedMs);
            Console.ReadLine();
        }
    }
}
