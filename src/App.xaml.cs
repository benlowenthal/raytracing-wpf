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

            Vector2[] uvs = new Vector2[] { new Vector2(0.5f, 0), new Vector2(0, 1), new Vector2(1, 1) };

            Object3D r = new Object3D(0, 0, 2, triV, triF, new Vector3(1, 0, 0));
            r.transform *= Matrix4x4.CreateRotationY(MathF.PI / 4);
            r.SetTextureProperties(Environment.CurrentDirectory + @"..\..\..\..\resources\brick.jpg", uvs, triF);
            Env.Add(r);

            Object3D l = new Object3D(0, 1, 2, triV, triF, new Vector3(0, 0, 1));
            l.transform *= Matrix4x4.CreateRotationY(-MathF.PI / 4);
            Env.Add(l);

            Vector3[] sqV = new Vector3[] { new Vector3(5, 0, 5), new Vector3(-5, 0, 5), new Vector3(-5, 0, -1), new Vector3(5, 0, -1) };
            uint[][] sqF = new uint[][] { new uint[] { 0, 1, 2 }, new uint[] { 0, 2, 3 } };

            Object3D obj = new Object3D(0, -1, 0, sqV, sqF, new Vector3(0.9f, 0.9f, 0.9f));
            obj.SetProperties(0.5f, 0, 1);
            Env.Add(obj);

            ObjReader.ReadFile(Environment.CurrentDirectory + @"..\..\..\..\resources\object.obj", out Object3D o1);
            o1.transform = Matrix4x4.CreateTranslation(0, 2, 2);
            Env.Add(o1);
            ObjReader.ReadFile(Environment.CurrentDirectory + @"..\..\..\..\resources\object.obj", out Object3D o2);
            o2.transform = Matrix4x4.CreateTranslation(3, 0, 1);
            Env.Add(o2);
            ObjReader.ReadFile(Environment.CurrentDirectory + @"..\..\..\..\resources\object.obj", out Object3D o3);
            o3.transform = Matrix4x4.CreateTranslation(-3, 0, 1);
            Env.Add(o3);

            Env.Add(new Emitter(5, 3, 1, Vector3.One, 2.5f));
        }
    }
}
