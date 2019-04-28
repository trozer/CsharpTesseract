using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTesseract4
{
    /// <summary>
    /// Represents the method used to rotate an image.
    /// </summary>
    public enum RotationMethod : int
    {
        /// <summary>
        /// Use area map rotation, if possible.
        /// </summary>
        AreaMap = 1,
        /// <summary>
        /// Use shear rotation.
        /// </summary>
        Shear = 2,
        /// <summary>
        /// Use sampling.
        /// </summary>
        Sampling = 3
    }
}
