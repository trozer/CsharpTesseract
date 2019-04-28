﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTesseract4
{
    public abstract class DisposableBase : IDisposable
    {
        static readonly TraceSource trace = new TraceSource("Tesseract");

        protected DisposableBase()
        {
            IsDisposed = false;
        }

        ~DisposableBase()
        {
            Dispose(false);
            trace.TraceEvent(TraceEventType.Warning, 0, "{0} was not disposed off.", this);
        }


        public void Dispose()
        {
            Dispose(true);

            IsDisposed = true;
            GC.SuppressFinalize(this);

            if (Disposed != null)
            {
                Disposed(this, EventArgs.Empty);
            }
        }

        public bool IsDisposed { get; private set; }

        public event EventHandler<EventArgs> Disposed;


        protected virtual void VerifyNotDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(ToString());
        }

        protected abstract void Dispose(bool disposing);
    }

}
