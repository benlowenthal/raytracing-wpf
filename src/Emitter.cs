using System;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RaytracingWPF
{
    class Emitter : Object3D
    {
        public float str;

        public Emitter(float x, float y, float z, Color c, float strength) : base(x, y, z)
        {
            str = strength;
            color = c;
        }
    }
}
