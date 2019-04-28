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
            using (var engine = new TesseractEngine("F:\\Work\\Tesseract\\CsharpTesseract\\CsharpTesseract4\\CsharpTesseract4\\CsharpTesseract4\\bin\\x64\\Debug\\tessdata", "hun",
                EngineMode.LSTMOnly, new string[0], new Dictionary<string, object>(), false))
            {

                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    Bitmap kutya = new Bitmap(@"F:\\Work\\Tesseract\\CsharpTesseract\\_leves.bmp");
                    InvokeTesseractOnBitmap(kutya, engine);
                    kutya.Dispose();
                }

                watch.Stop();
                Console.WriteLine(watch.ElapsedMilliseconds);

            }

            Console.ReadLine();
        }

        private static void InvokeTesseractOnBitmap(Bitmap bitmap, TesseractEngine engine)
        {
            BitmapToPixConverter converter = new BitmapToPixConverter();
            using (var img = converter.Convert(bitmap))
            {
                using (var page = engine.Process(img))
                {
                    var text = page.GetText();
                    Console.WriteLine(text);
                }
            }
        }
    }
}
