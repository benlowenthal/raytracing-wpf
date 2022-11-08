using System;
using System.Collections.Generic;
using System.Numerics;

namespace RaytracingWPF
{
    struct Ray
    {
        public Vector3 start;
        public Vector3 dir;
        public float t;
        public uint tri;

        public Ray(Vector3 origin, Vector3 direc)
        {
            start = origin;
            dir = direc;
            t = float.MaxValue;
            tri = 0;
        }
    }

    static class RayCast
    {
        private const float EPSILON = 0.000001f;

        public static bool Cast(ref Ray ray)
        {
            return BVHTree(ref ray, 0);
        }

        public static bool BVHTree(ref Ray ray, uint idx)
        {
            BVHNode node = BVH.nodes[idx];

            if (!AABB(ray, node.aabb.min, node.aabb.max)) return false;

            if (node.triCount > 0) //node is leaf
            {
                bool hit = false;
                for (uint i = 0; i < node.triCount; i++)
                {
                    if (Triangle(ref ray, node.triOffset + i, out float t))
                    {
                        ray.t = t;
                        ray.tri = node.triOffset + i;
                        hit = true;
                    }
                }
                return hit;
            }
            else //node has sub nodes
            {
                bool b1 = BVHTree(ref ray, node.childPtr);
                bool b2 = BVHTree(ref ray, node.childPtr + 1);
                return b1 || b2;
            }
        }

        public static bool AABB(Ray ray, Vector3 min, Vector3 max)
        {
            Vector3 fmin = (min - ray.start) / ray.dir;
            Vector3 fmax = (max - ray.start) / ray.dir;

            float tmin = Math.Max(Math.Max(Math.Min(fmin.X, fmax.X), Math.Min(fmin.Y, fmax.Y)), Math.Min(fmin.Z, fmax.Z));
            float tmax = Math.Min(Math.Min(Math.Max(fmin.X, fmax.X), Math.Max(fmin.Y, fmax.Y)), Math.Max(fmin.Z, fmax.Z));

            return tmax >= tmin && tmax > 0 && tmin < ray.t;
        }

        public static bool Triangle(ref Ray ray, uint triIdx, out float t)
        {
            t = 0;
            Vector3[] face = BVH.tris[triIdx].v;

            Vector3 e1 = face[1] - face[0];
            Vector3 e2 = face[2] - face[0];
            Vector3 h = Vector3.Cross(ray.dir, e2);

            float a = Vector3.Dot(e1, h);
            if (a > -EPSILON && a < EPSILON) return false; //parallel ray

            float f = 1 / a;
            Vector3 s = ray.start - face[0];

            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false; //out of range

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.dir, q);
            if (v < 0f || u + v > 1f) return false; //

            t = f * Vector3.Dot(e2, q);
            return t > EPSILON && t < ray.t; //ray hit
        }
    }
}
