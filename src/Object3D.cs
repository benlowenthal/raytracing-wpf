using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace RaytracingWPF
{
    class Object3D
    {
        public Matrix4x4 transform;

        public Vector3[] verts;
        public Vector2[] uvs;
        public uint[][] faces;
        public uint[][] uvIdx;

        public Vector3 color;
        public byte[] texture;
        public int texW;
        public int texH;

        public float gloss = 0;
        public float transparent = 0;
        public float ri = 1;

        public Object3D(float x, float y, float z)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
        }

        public Object3D(float x, float y, float z, Vector3[] v, uint[][] f, Vector3 c)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
            verts = v;
            color = c;
            faces = f;
        }

        public void SetProperties(float gl, float tr, float re)
        {
            gloss = Math.Clamp(gl, 0, 1);
            transparent = Math.Clamp(tr, 0, 1);
            ri = Math.Max(re, 1);
        }

        public bool SetTextureProperties(string path, Vector2[] t, uint[][] tIdx)
        {
            uvIdx = tIdx;
            uvs = t;

            try
            {
                FileStream fs = new FileStream(path, FileMode.Open);
                BitmapDecoder dec;

                if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
                    dec = new JpegBitmapDecoder(fs, BitmapCreateOptions.None, BitmapCacheOption.Default);
                else if (path.EndsWith(".png"))
                    dec = new PngBitmapDecoder(fs, BitmapCreateOptions.None, BitmapCacheOption.Default);
                else
                {
                    System.Diagnostics.Trace.WriteLine("Unknown file type: " + path);
                    return false;
                }

                FormatConvertedBitmap fcb = new FormatConvertedBitmap(dec.Frames[0], PixelFormats.Rgb24, null, 0);

                texW = dec.Frames[0].PixelWidth;
                texH = dec.Frames[0].PixelHeight;
                texture = new byte[texW * texH * 3];
                fcb.CopyPixels(texture, texW * 3, 0);

                fs.Close();
            }
            catch (IOException)
            {
                System.Diagnostics.Trace.WriteLine("File not found: " + path);
                return false;
            }

            return true;
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

        public Vector3[] WorldSpace(uint[] face)
        {
            Vector3[] world = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                Vector4 x = Apply(new Vector4(verts[face[i]], 1), transform);
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
