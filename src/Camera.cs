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
    class Camera
    {
        public Matrix4x4 transform;
        public int width;
        public int height;
        public float vFov;
        public float hFov;

        private const int MAX_BOUNCES = 1;

        private byte[] buffer;

        public Camera(float x, float y, float z, int w, int h, int fieldOfView)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
            width = w;
            height = h;
            buffer = new byte[w * h * 3];
            vFov = fieldOfView * (MathF.PI / 180f);
            hFov = h * fieldOfView / w * (MathF.PI / 180f);
        }

        public byte[] CreateImage()
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    //find ray direction at i,j
                    Vector4 dir = Object3D.Apply(new Vector4(0, 0, 1, 0), Matrix4x4.CreateFromYawPitchRoll(i * vFov / width - vFov / 2f, j * hFov / height - hFov / 2f, 0));
                    dir = ApplyTransform(dir);
                    
                    //calculate per-pixel colour
                    Color c = CastRay(transform.Translation, new Vector3(dir.X, dir.Y, dir.Z), MAX_BOUNCES);

                    buffer[j * width * 3 + i * 3 + 0] = c.R;
                    buffer[j * width * 3 + i * 3 + 1] = c.G;
                    buffer[j * width * 3 + i * 3 + 2] = c.B;
                }
            }
            return buffer;
        }

        private Color CastRay(Vector3 start, Vector3 dir, int bounces)
        {
            Ray ray = new Ray(start, Vector3.Normalize(dir));

            if (RayCast.Cast(ref ray))
            {
                Object3D hitObj = Env.objects[BVH.tris[ray.tri].obj];
                Vector3 hitPoint = ray.start + (ray.dir * ray.t);

                //reflection
                Color c = hitObj.color;
                if (bounces > 0)
                    c += CastRay(hitPoint, Vector3.Reflect(dir, ray.normal), bounces - 1) * (1 / ray.t);

                //refraction
                Color r = Colors.Black;
                if (hitObj.refract != 1)
                    r += CastRay(hitPoint, Vector3.Reflect(dir, ray.normal), bounces - 1) * (1 / ray.t);

                //shadows
                Color s = Colors.Black;
                foreach (Emitter e in Env.lights)
                    s += CastLightRay(hitPoint, e);

                return Color.FromScRgb(1, c.ScR * s.ScR, c.ScG * s.ScG, c.ScB * s.ScB);
            }

            return new Color();
        }

        private Color CastLightRay(Vector3 start, Emitter e)
        {
            Ray ray = new Ray(start , e.transform.Translation - start);

            if (RayCast.Cast(ref ray)) return new Color();
                        
            return e.color * e.str * (1 / (e.transform.Translation - start).Length());
        }

        public Vector4 ApplyTransform(Vector4 v)
        {
            return Object3D.Apply(v, transform);
        }
    }
}
