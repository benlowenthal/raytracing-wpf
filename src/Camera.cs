using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;

namespace RaytracingWPF
{
    class Camera
    {
        public Matrix4x4 transform;
        public int width;
        public int height;
        public float vFov;
        public float hFov;

        private Random r = new Random();

        private const int TILE_SIZE = 16;
        private const int MAX_BOUNCES = 1;

        private byte[] buffer;
        private byte[] causticBuffer;

        public Camera(float x, float y, float z, int w, int h, int fieldOfView)
        {
            transform = Matrix4x4.CreateTranslation(x, y, z);
            width = w;
            height = h;
            buffer = new byte[w * h * 3];

            hFov = fieldOfView * (MathF.PI / 180f);
            vFov = h * fieldOfView / w * (MathF.PI / 180f);
        }

        public byte[] CreateImage()
        {
            for (int y = 0; y < height; y += TILE_SIZE) for (int x = 0; x < width; x += TILE_SIZE)
            {
                for (int b = 0; b < TILE_SIZE && y + b < height; b++) for (int a = 0; a < TILE_SIZE && x + a < width; a++)
                {
                    int i = x + a;
                    int j = y + b;

                    //find ray direction at i,j
                    Vector4 dir = Object3D.Apply(new Vector4(0, 0, 1, 0), Matrix4x4.CreateFromYawPitchRoll(i * hFov / width - hFov / 2f, j * vFov / height - vFov / 2f, 0));
                    dir = ApplyTransform(dir);
                    
                    //calculate per-pixel colour
                    Vector3 c = CastRay(transform.Translation, new Vector3(dir.X, dir.Y, dir.Z), MAX_BOUNCES);

                    int idx = j * width * 3 + i * 3;
                    buffer[idx + 0] = (byte) Math.Clamp((int)(c.X * 255), 0, 255);
                    buffer[idx + 1] = (byte) Math.Clamp((int)(c.Y * 255), 0, 255);
                    buffer[idx + 2] = (byte) Math.Clamp((int)(c.Z * 255), 0, 255);
                }
            }
            return buffer;
        }

        private Vector3 CastRay(Vector3 start, Vector3 dir, int bounces)
        {
            Ray ray = new Ray(start, dir);

            if (RayCast.Cast(ref ray))
            {
                Tri hitTri = BVH.tris[ray.tri];
                Object3D hitObj = Env.objects[hitTri.obj];
                Vector3 hitPoint = ray.start + (ray.dir * ray.t);

                //initial colour
                Vector3 c = hitObj.color;
                if (hitObj.texture != null)
                {
                    float denom = (hitTri.v[0].X - hitTri.v[2].X) * (hitTri.v[1].Y - hitTri.v[2].Y) + (hitTri.v[2].X - hitTri.v[1].X) * (hitTri.v[0].Y - hitTri.v[2].Y);
                    float w1 = ((hitPoint.X - hitTri.v[2].X) * (hitTri.v[1].Y - hitTri.v[2].Y) + (hitTri.v[2].X - hitTri.v[1].X) * (hitPoint.Y - hitTri.v[2].Y)) / denom;
                    float w2 = ((hitPoint.X - hitTri.v[2].X) * (hitTri.v[2].Y - hitTri.v[0].Y) + (hitTri.v[0].X - hitTri.v[2].X) * (hitPoint.Y - hitTri.v[2].Y)) / denom;
                    float w3 = 1 - w1 - w2;

                    Vector2 uv = hitObj.uvs[hitTri.uv[0]] * w1 + hitObj.uvs[hitTri.uv[1]] * w2 + hitObj.uvs[hitTri.uv[2]] * w3;

                    int idx = (int)(uv.Y * hitObj.texH) * hitObj.texW * 3 + (int)(uv.X * hitObj.texW) * 3;
                    c = new Vector3(
                        hitObj.texture[idx + 0] / 255f,
                        hitObj.texture[idx + 1] / 255f,
                        hitObj.texture[idx + 2] / 255f
                    );
                }

                //reflection
                Vector3 rl = Vector3.Zero;
                if (hitObj.gloss > 0 && bounces > 0)
                {
                    c *= 1 - hitObj.gloss;
                    rl = CastRay(hitPoint, Vector3.Reflect(ray.dir, hitTri.normal), bounces - 1) * hitObj.gloss;
                }

                //refraction
                Vector3 rf = Vector3.Zero;
                if (hitObj.transparent > 0)
                {
                    c *= 1 - hitObj.transparent;
                    if (Vector3.Dot(ray.dir, hitTri.normal) < 0) //front face
                        rf = CastRay(hitPoint, Refract(ray.dir, -hitTri.normal, hitObj.ri), bounces) * hitObj.transparent;
                    else //back face
                        rf = CastRay(hitPoint, Refract(ray.dir, hitTri.normal, 1f / hitObj.ri), bounces);
                }

                //shadows
                Vector3 s = Vector3.Zero;
                foreach (Emitter e in Env.lights)
                {
                    //Vector3.Reflect(ray.dir, hitTri.normal)
                    s += CastShadowRay(hitPoint, e);
                    s += CastShadowRay(hitPoint, e);
                    s += CastShadowRay(hitPoint, e);
                    s *= 0.33334f;
                }

                return (c * s) + (rl + rf);
            }

            return Vector3.Zero;
        }

        private Vector3 CastShadowRay(Vector3 start, Emitter e)
        {
            float dv = 1 / 250f;
            Vector3 dir = e.transform.Translation - start + new Vector3((r.Next(100) - 50) * dv, (r.Next(100) - 50) * dv, (r.Next(100) - 50) * dv);
            Ray ray = new Ray(start, dir);

            if (RayCast.Cast(ref ray) && (ray.t * ray.t) < dir.LengthSquared()) return Vector3.Zero;

            //diffuse
            return e.color * (e.str * 10 / (dir.LengthSquared() + 1));
        }

        private bool CastLightRay(Vector3 start, Vector3 dir, out Vector3 hit)
        {
            hit = Vector3.Zero;
            Ray ray = new Ray(start, dir);

            if (RayCast.Cast(ref ray))
            {
                Object3D hitObj = Env.objects[BVH.tris[ray.tri].obj];
                Vector3 hitNormal = BVH.tris[ray.tri].normal;
                hit = ray.start + (ray.dir * ray.t);

                if (hitObj.transparent > 0)
                {
                    if (Vector3.Dot(ray.dir, hitNormal) < 0) //front face
                        return CastLightRay(hit, Refract(ray.dir, -hitNormal, hitObj.ri), out hit);
                    else //back face
                        return CastLightRay(hit, Refract(ray.dir, hitNormal, 1f / hitObj.ri), out hit);
                }
                return true;
            }
            return false;
        }

        private bool CastCameraRay(Vector3 start, out int x, out int y)
        {
            x = 0; y = 0;
            Ray ray = new Ray(start, transform.Translation - start);

            if (RayCast.Cast(ref ray) && (ray.t * ray.t) < ray.dir.LengthSquared()) return false;

            Vector4 f = ApplyTransform(new Vector4(0, 0, 1, 0));
            Vector3 fwd = new Vector3(f.X, f.Y, f.Z);

            Vector3 xnorm = new Vector3(0, 1, 0);
            Vector3 xcomp = -ray.dir - (xnorm * Vector3.Dot(-ray.dir, xnorm));

            Vector3 ynorm = new Vector3(1, 0, 0);
            Vector3 ycomp = -ray.dir - (ynorm * Vector3.Dot(-ray.dir, ynorm));

            float xtheta = MathF.Acos(Vector3.Dot(xcomp, fwd) / (xcomp.Length() * fwd.Length()));
            float ytheta = MathF.Acos(Vector3.Dot(ycomp, fwd) / (ycomp.Length() * fwd.Length()));

            if (xtheta > hFov / 2f || xtheta < -hFov / 2f || ytheta > vFov / 2f || ytheta < -vFov / 2f) return false;

            x = (int)(width * xtheta / hFov + width / 2f);
            y = (int)(height * ytheta / vFov + height / 2f);
            return true;
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
