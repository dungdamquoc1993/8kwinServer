using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct SingleHelper
    {
        public SingleHelper(float f)
        {
            this.i = 0;
            this.f = f;
        }


        [System.Runtime.InteropServices.FieldOffset(0)]
        public int i;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public float f;

    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct FloatHelper
    {

        public FloatHelper(double f)
        {
            this.i = ((long)(0));
            this.f = f;
        }


        [System.Runtime.InteropServices.FieldOffset(0)]
        public long i;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public double f;
    }
}
