using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RaytracingWPF
{
    class Object3D
    {
        public Matrix4x4 transform;

        public Vector3[] verts;
        public Face[] faces;

        public Color color;
        public float metallic = 0;
        public float refract = 0;

        public Object3D(float x, float y, float z)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
        }

        public Object3D(float x, float y, float z, Vector3[] v, uint[][] f, Color c, float metal, float re)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
            verts = v;
            color = c;
            metallic = Math.Clamp(metal, 0, 1);
            refract = Math.Max(re, 0);

            List<Face> fs = new List<Face>();
            foreach (uint[] face in f)
                fs.Add(new Face(this, face));
            faces = fs.ToArray();
        }

        public static Vector4 Apply(Vector4 v, Matrix4x4 m)
        {
            return new Vector4(
                v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + v.W * m.M41,
                v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + v.W * m.M42,
                v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + v.W * m.M43,
                v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + v.W * m.M44
            );
        }

        public Vector3[] WorldSpace(Face face)
        {
            Vector3[] world = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                Vector4 x = Apply(new Vector4(verts[face.vIdx[i]], 1), transform);
                world[i] = new Vector3(x.X, x.Y, x.Z);
            }
            return world;
        }

        public Vector3 WorldSpace(Vector3 point)
        {
            Vector4 x = Apply(new Vector4(point, 1), transform);
            return new Vector3(x.X, x.Y, x.Z);
        }
    }
}
