using CsharpTesseract4.Interop;
using InteropDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTesseract4
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = new Program();
            var native = InteropRuntimeImplementer.CreateInstance<ITessApiSignatures>();
            var handle = new HandleRef(program, native.BaseApiCreate());
            native.BaseApiInit(handle, "F:\\Work\\Tesseract\\CsharpTesseract\\CsharpTesseract4", "hu");
            Console.ReadLine();
        }
    }
}
