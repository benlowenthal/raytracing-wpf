using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace RaytracingWPF
{
    public partial class App : Application
    {
        public App()
        {
            Vector3[] triV = new Vector3[] { new Vector3(0, 1, 0), new Vector3(-1.6f, -1, 0), new Vector3(1.6f, -1, 0) };
            uint[][] triF = new uint[][] { new uint[] { 0, 1, 2 } };

            Object3D r = new Object3D(0, 1, 2, triV, triF, Colors.Red, 0, 1);
            r.transform *= Matrix4x4.CreateRotationY(MathF.PI / 2);
            Env.Add(r);

            Object3D l = new Object3D(0, 1, -2, triV, triF, Colors.Blue, 0, 1);
            l.transform *= Matrix4x4.CreateRotationY(MathF.PI / 2);
            Env.Add(l);

            Vector3[] sqV = new Vector3[] { new Vector3(3, 0, 3), new Vector3(-3, 0, 3), new Vector3(-3, 0, -3), new Vector3(3, 0, -3) };
            uint[][] sqF = new uint[][] { new uint[] { 0, 1, 2 }, new uint[] { 0, 2, 3 } };

            Env.Add(new Object3D(0, -1, 0, sqV, sqF, Colors.Gray, 0, 1));

            Env.Add(new Emitter(4, 2, -1, Colors.White, 1));
        }
    }
}
