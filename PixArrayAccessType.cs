using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTesseract4
{
    /// <summary>
    /// Determines how <see cref="Pix"/> of a <see cref="PixArray"/> structure are accessed.
    /// </summary>
    public enum PixArrayAccessType : int
    {
        /// <summary>
        /// Stuff it in; no copy, clone or copy-clone.
        /// </summary>
        Insert = 0,

        /// <summary>
        ///	Make/use a copy of the object.
        /// </summary>
        Copy = 1,

        /// <summary>
        /// Make/use clone (ref count) of the object   
        /// </summary>
        Clone = 2,

        /// <summary>
        /// Make a new object and fill with with clones of each object in the array(s) 
        /// </summary>
        CopyClone = 3
    }
}
