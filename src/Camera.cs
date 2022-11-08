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
                Vector3 hitNormal = BVH.tris[ray.tri].normal;

                //reflection
                Color c = hitObj.color;
                if (hitObj.gloss > 0 && bounces > 0)
                {
                    c *= 1 - hitObj.gloss;
                    c += CastRay(hitPoint, Vector3.Reflect(ray.dir, hitNormal), bounces - 1) * hitObj.gloss;
                }

                //refraction
                if (hitObj.transparent > 0)
                {
                    c *= 1 - hitObj.transparent;
                    if (Vector3.Dot(ray.dir, hitNormal) < 0) //front face
                        c += CastRay(hitPoint, Refract(ray.dir, -hitNormal, hitObj.ri), bounces) * hitObj.transparent;
                    else //back face
                        c += CastRay(hitPoint, Refract(ray.dir, hitNormal, 1 / hitObj.ri), bounces);
                    return c;
                }

                //shadows
                Color s = Colors.Black;
                foreach (Emitter e in Env.lights)
                {
                    s += CastLightRay(hitPoint, Vector3.Reflect(ray.dir, hitNormal), e);
                    s += CastLightRay(hitPoint, Vector3.Reflect(ray.dir, hitNormal), e);
                    s *= 0.5f;
                }

                return Color.FromScRgb(1, c.ScR * s.ScR, c.ScG * s.ScG, c.ScB * s.ScB);
            }

            return Colors.Black;
        }

        private Color CastLightRay(Vector3 start, Vector3 reflect, Emitter e)
        {
            Random r = new Random();
            Vector3 dir = e.transform.Translation - start + new Vector3(r.Next(100) / 250f, r.Next(100) / 250f, r.Next(100) / 250f);
            Ray ray = new Ray(start, dir);

            if (RayCast.Cast(ref ray) && (ray.t * ray.t) < dir.LengthSquared()) return new Color();

            //specular
            if ((Vector3.Normalize(ray.dir) - Vector3.Normalize(reflect)).LengthSquared() < 0.0001f) return e.color * e.str;

            //diffuse
            return e.color * e.str * (1 / dir.Length());
        }

        private Vector3 Refract(Vector3 i, Vector3 n, float ri)
        {
            float dot = Vector3.Dot(i, n);
            return (n * MathF.Sqrt(1 - (ri * ri * (1 - (dot * dot))))) + ((i - (n * dot)) * ri);
        }

        public Vector4 ApplyTransform(Vector4 v)
        {
            return Object3D.Apply(v, transform);
        }
    }
}
