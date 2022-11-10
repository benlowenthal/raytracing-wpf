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

            Object3D r = new Object3D(0, 0, 2, triV, triF, new Vector3(1, 0, 0));
            r.transform *= Matrix4x4.CreateRotationY(MathF.PI / 4);
            Env.Add(r);

            Object3D l = new Object3D(0, 1, 2, triV, triF, new Vector3(0, 0, 1));
            l.transform *= Matrix4x4.CreateRotationY(-MathF.PI / 4);
            Env.Add(l);

            Vector3[] sqV = new Vector3[] { new Vector3(5, 0, 5), new Vector3(-5, 0, 5), new Vector3(-5, 0, -1), new Vector3(5, 0, -1) };
            uint[][] sqF = new uint[][] { new uint[] { 0, 1, 2 }, new uint[] { 0, 2, 3 } };

            Object3D obj = new Object3D(0, -1, 0, sqV, sqF, new Vector3(0.9f, 0.9f, 0.9f));
            obj.SetProperties(0.5f, 0, 1);
            Env.Add(obj);

            Env.Add(new Emitter(5, 3, 1, Vector3.One, 2.5f));
        }
    }
}
