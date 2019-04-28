using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Tesseract.Internal;

namespace CsharpTesseract4
{
    public class TesseractEngine : DisposableBase
    {
        private int processCount = 0;
        private static readonly TraceSource trace = new TraceSource("Tesseract");
        private HandleRef handle;
        public TesseractEngine(string datapath, string language, EngineMode engineMode, IEnumerable<string> configFiles, IDictionary<string, object> initialOptions, bool setOnlyNonDebugVariables)
        {
            Guard.RequireNotNullOrEmpty("language", language);

            DefaultPageSegMode = PageSegMode.Auto;
            handle = new HandleRef(this, Interop.TessApi.Native.BaseApiCreate());

            Initialize(datapath, language, engineMode, configFiles, initialOptions, setOnlyNonDebugVariables);
        }

        private void Initialize(string datapath, string language, EngineMode engineMode, IEnumerable<string> configFiles, IDictionary<string, object> initialValues, bool setOnlyNonDebugVariables)
        {
            const string TessDataDirectory = "tessdata";
            Guard.RequireNotNullOrEmpty("language", language);

            // do some minor processing on datapath to fix some common errors (this basically mirrors what tesseract does as of 3.04)
            if (!String.IsNullOrEmpty(datapath))
            {
                // remove any excess whitespace
                datapath = datapath.Trim();

                // remove any trialing '\' or '/' characters
                if (datapath.EndsWith("\\", StringComparison.Ordinal) || datapath.EndsWith("/", StringComparison.Ordinal))
                {
                    datapath = datapath.Substring(0, datapath.Length - 1);
                }
                // remove 'tessdata', if it exists, tesseract will add it when building up the tesseract path
                if (datapath.EndsWith("tessdata", StringComparison.OrdinalIgnoreCase))
                {
                    datapath = datapath.Substring(0, datapath.Length - TessDataDirectory.Length);
                }
            }

            if (Interop.TessApi.BaseApiInit(handle, datapath, language, (int)engineMode, configFiles ?? new List<string>(), initialValues ?? new Dictionary<string, object>(), setOnlyNonDebugVariables) != 0)
            {
                // Special case logic to handle cleaning up as init has already released the handle if it fails.
                handle = new HandleRef(this, IntPtr.Zero);
                GC.SuppressFinalize(this);

                throw new TesseractException(ErrorMessage.Format(1, "Failed to initialise tesseract engine."));
            }
        }

        internal HandleRef Handle
        {
            get { return handle; }
        }

        /// <summary>
        /// Processes the specific image.
        /// </summary>
        /// <remarks>
        /// You can only have one result iterator open at any one time.
        /// </remarks>
        /// <param name="image">The image to process.</param>
        /// <param name="pageSegMode">The page layout analyasis method to use.</param>
        public Page Process(Pix image, PageSegMode? pageSegMode = null)
        {
            return Process(image, null, new Rect(0, 0, image.Width, image.Height), pageSegMode);
        }

        /// <summary>
        /// Processes a specified region in the image using the specified page layout analysis mode.
        /// </summary>
        /// <remarks>
        /// You can only have one result iterator open at any one time.
        /// </remarks>
        /// <param name="image">The image to process.</param>
        /// <param name="region">The image region to process.</param>
        /// <param name="pageSegMode">The page layout analyasis method to use.</param>
        /// <returns>A result iterator</returns>
        public Page Process(Pix image, Rect region, PageSegMode? pageSegMode = null)
        {
            return Process(image, null, region, pageSegMode);
        }

        /// <summary>
        /// Processes the specific image.
        /// </summary>
        /// <remarks>
        /// You can only have one result iterator open at any one time.
        /// </remarks>
        /// <param name="image">The image to process.</param>
        /// <param name="inputName">Sets the input file's name, only needed for training or loading a uzn file.</param>
        /// <param name="pageSegMode">The page layout analyasis method to use.</param>
        public Page Process(Pix image, string inputName, PageSegMode? pageSegMode = null)
        {
            return Process(image, inputName, new Rect(0, 0, image.Width, image.Height), pageSegMode);
        }

        /// <summary>
        /// Processes a specified region in the image using the specified page layout analysis mode.
        /// </summary>
        /// <remarks>
        /// You can only have one result iterator open at any one time.
        /// </remarks>
        /// <param name="image">The image to process.</param>
        /// <param name="inputName">Sets the input file's name, only needed for training or loading a uzn file.</param>
        /// <param name="region">The image region to process.</param>
        /// <param name="pageSegMode">The page layout analyasis method to use.</param>
        /// <returns>A result iterator</returns>
        public Page Process(Pix image, string inputName, Rect region, PageSegMode? pageSegMode = null)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (region.X1 < 0 || region.Y1 < 0 || region.X2 > image.Width || region.Y2 > image.Height)
                throw new ArgumentException("The image region to be processed must be within the image bounds.", "region");
            if (processCount > 0) throw new InvalidOperationException("Only one image can be processed at once. Please make sure you dispose of the page once your finished with it.");

            processCount++;

            var actualPageSegmentMode = pageSegMode.HasValue ? pageSegMode.Value : DefaultPageSegMode;
            Interop.TessApi.Native.BaseAPISetPageSegMode(handle, actualPageSegmentMode);
            Interop.TessApi.Native.BaseApiSetImage(handle, image.Handle);
            if (!String.IsNullOrEmpty(inputName))
            {
                Interop.TessApi.Native.BaseApiSetInputName(handle, inputName);
            }
            var page = new Page(this, image, inputName, region, actualPageSegmentMode);
            page.Disposed += OnIteratorDisposed;
            return page;
        }

        #region Config
        public PageSegMode DefaultPageSegMode
        {
            get;
            set;
        }


        protected override void Dispose(bool disposing)
        {
            if (handle.Handle != IntPtr.Zero)
            {
                Interop.TessApi.Native.BaseApiDelete(handle);
                handle = new HandleRef(this, IntPtr.Zero);
            }
        }

        private string GetTessDataPrefix()
        {
            try
            {
                return Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            }
            catch (SecurityException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, "Failed to detect if the environment variable 'TESSDATA_PREFIX' is set: {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Ties the specified pix to the lifecycle of a page.
        /// </summary>
        private class PageDisposalHandle
        {
            private readonly Page page;
            private readonly Pix pix;

            public PageDisposalHandle(Page page, Pix pix)
            {
                this.page = page;
                this.pix = pix;
                page.Disposed += OnPageDisposed;
            }

            private void OnPageDisposed(object sender, System.EventArgs e)
            {
                page.Disposed -= OnPageDisposed;
                // dispose the pix when the page is disposed.
                pix.Dispose();
            }
        }

        public bool SetDebugVariable(string name, string value)
        {
            return Interop.TessApi.BaseApiSetDebugVariable(handle, name, value) != 0;
        }

        /// <summary>
        /// Sets the value of a string variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The new value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool SetVariable(string name, string value)
        {
            return Interop.TessApi.BaseApiSetVariable(handle, name, value) != 0;
        }

        /// <summary>
        /// Sets the value of a boolean variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The new value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool SetVariable(string name, bool value)
        {
            var strEncodedValue = value ? "TRUE" : "FALSE";
            return Interop.TessApi.BaseApiSetVariable(handle, name, strEncodedValue) != 0;
        }

        /// <summary>
        /// Sets the value of a integer variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The new value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool SetVariable(string name, int value)
        {
            var strEncodedValue = value.ToString("D", CultureInfo.InvariantCulture.NumberFormat);
            return Interop.TessApi.BaseApiSetVariable(handle, name, strEncodedValue) != 0;
        }

        /// <summary>
        /// Sets the value of a double variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The new value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool SetVariable(string name, double value)
        {
            var strEncodedValue = value.ToString("R", CultureInfo.InvariantCulture.NumberFormat);
            return Interop.TessApi.BaseApiSetVariable(handle, name, strEncodedValue) != 0;
        }

        /// <summary>
        /// Attempts to retrieve the value for a boolean variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The current value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool TryGetBoolVariable(string name, out bool value)
        {
            int val;
            if (Interop.TessApi.Native.BaseApiGetBoolVariable(handle, name, out val) != 0)
            {
                value = (val != 0);
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve the value for a double variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The current value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool TryGetDoubleVariable(string name, out double value)
        {
            return Interop.TessApi.Native.BaseApiGetDoubleVariable(handle, name, out value) != 0;
        }

        /// <summary>
        /// Attempts to retrieve the value for an integer variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The current value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool TryGetIntVariable(string name, out int value)
        {
            return Interop.TessApi.Native.BaseApiGetIntVariable(handle, name, out value) != 0;
        }

        /// <summary>
        /// Attempts to retrieve the value for a string variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The current value of the variable.</param>
        /// <returns>Returns <c>True</c> if successful; otherwise <c>False</c>.</returns>
        public bool TryGetStringVariable(string name, out string value)
        {
            value = Interop.TessApi.BaseApiGetStringVariable(handle, name);
            return value != null;
        }

        /// <summary>
        /// Attempts to print the variables to the file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool TryPrintVariablesToFile(string filename)
        {
            return Interop.TessApi.Native.BaseApiPrintVariablesToFile(handle, filename) != 0;
        }

#endregion Config

        #region Event Handlers

        private void OnIteratorDisposed(object sender, EventArgs e)
        {
            processCount--;
        }

        #endregion Event Handlers
    }
}
