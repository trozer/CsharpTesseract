using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTesseract4
{
    public enum EngineMode : int
    {
        TesseractOnly = 0,
        LSTMOnly,
        LegacyPlusLSTM,
        Default
    }
}
