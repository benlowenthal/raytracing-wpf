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
    struct BVHNode
    {
        public (Vector3 min, Vector3 max) aabb;
        public uint childPtr;
        public uint triOffset;
        public uint triCount;
    }

    static class BVH
    {
        private static uint n;
        private static uint used;
        public static BVHNode[] nodes;
        public static Tri[] tris;

        public static bool Build()
        {
            n = 0;
            used = 0;

            List<Tri> t = new List<Tri>();
            for (int i = 0; i < Env.objects.Count; i++)
            {
                Object3D obj = Env.objects[i];
                for (int j = 0; j < obj.faces.Length; j++)
                {
                    uint[] uvs = obj.uvIdx != null ? obj.uvIdx[j] : null;
                    t.Add(new Tri(i, uvs, obj.WorldSpace(obj.faces[j])));
                    n++;
                }
            }

            if (n < 1) return false;
            tris = t.ToArray();
            nodes = new BVHNode[2 * n - 1];

            uint idx = 0;
            nodes[idx].childPtr = 0;
            nodes[idx].triOffset = 0;
            nodes[idx].triCount = n;
            AABB(idx);

            Subdivide(idx);

            uint end = 0;
            while (nodes[end].childPtr != 0) end = nodes[end].childPtr + 1; //follow right child until end

            //truncate empty space in array
            BVHNode[] nodesTemp = new BVHNode[end + 1];
            for (uint i = 0; i <= end; i++) nodesTemp[i] = nodes[i];
            nodes = nodesTemp;
            foreach (BVHNode n in nodes) System.Diagnostics.Trace.WriteLine(n.triCount + "\t" + n.aabb.min + " -> " + n.aabb.max);

            return true;
        }

        private static void Subdivide(uint idx)
        {
            BVHNode node = nodes[idx];

            //estimate best split position/axis
            uint splitAxis = 0;
            float splitPos = 0;
            float minCost = float.MaxValue;
            for (uint axis = 0; axis < 3; axis++)
            {
                float start = GetAxis(node.aabb.min, axis);
                float incr = GetAxis(node.aabb.max - node.aabb.min, axis) / 16f;

                for (uint x = 1; x < 16; x++) //subdivisions
                {
                    float pos = start + (incr * x);
                    float cost = SplitHeuristic(idx, pos, axis);
                    if (cost < minCost)
                    {
                        splitAxis = axis;
                        splitPos = pos;
                        minCost = cost;
                    }
                }
            }

            //aabb is at optimum smallest size
            float parentCost = node.triCount * AABBArea(node.aabb.min, node.aabb.max);
            if (minCost >= parentCost) return;

            //quick sort partition
            uint i = node.triOffset;
            uint j = i + node.triCount - 1;
            while (i <= j)
            {
                if (GetAxis(tris[i].centroid, splitAxis) < splitPos) i++;
                else
                {
                    //swap tri positions
                    Tri temp = tris[i];
                    tris[i] = tris[j];
                    tris[j] = temp;

                    j--;
                }
            }

            uint l = i - node.triOffset;
            if (l == 0 || l == node.triCount) return; //no subdivision made - leaf node reached

            nodes[idx].childPtr = used + 1;

            nodes[used + 1].triOffset = node.triOffset;
            nodes[used + 1].triCount = l;
            AABB(used + 1);

            nodes[used + 2].triOffset = i;
            nodes[used + 2].triCount = node.triCount - l;
            AABB(used + 2);

            nodes[idx].triCount = 0;
            used += 2;

            Subdivide(used + 1);
            Subdivide(used + 2);
        }

        private static float SplitHeuristic(uint idx, float pos, uint axis)
        {
            uint i = 0;
            uint j = 0;

            (Vector3 lMin, Vector3 lMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            (Vector3 rMin, Vector3 rMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));

            for (uint x = 0; x < nodes[idx].triCount; x++)
            {
                Tri t = tris[nodes[idx].triOffset + x];
                if (GetAxis(t.centroid, axis) < pos)
                {
                    foreach (Vector3 v in t.v)
                    {
                        lMin.X = Math.Min(lMin.X, v.X);
                        lMin.Y = Math.Min(lMin.Y, v.Y);
                        lMin.Z = Math.Min(lMin.Z, v.Z);

                        lMax.X = Math.Max(lMax.X, v.X);
                        lMax.Y = Math.Max(lMax.Y, v.Y);
                        lMax.Z = Math.Max(lMax.Z, v.Z);
                    }
                    i++;
                }
                else
                {
                    foreach (Vector3 v in t.v)
                    {
                        rMin.X = Math.Min(rMin.X, v.X);
                        rMin.Y = Math.Min(rMin.Y, v.Y);
                        rMin.Z = Math.Min(rMin.Z, v.Z);

                        rMax.X = Math.Max(rMax.X, v.X);
                        rMax.Y = Math.Max(rMax.Y, v.Y);
                        rMax.Z = Math.Max(rMax.Z, v.Z);
                    }
                    j++;
                }
            }

            float cost = i * AABBArea(lMin, lMax) + j * AABBArea(rMin, rMax);
            if (cost == 0) return float.MaxValue; //avoid low cost for empty AABBs
            return cost;
        }

        private static void AABB(uint idx)
        {
            Vector3 min = new Vector3(int.MaxValue);
            Vector3 max = new Vector3(int.MinValue);
            for (uint i = 0; i < nodes[idx].triCount; i++)
            {
                Vector3[] verts = tris[nodes[idx].triOffset + i].v;

                foreach (Vector3 v in verts)
                {
                    min.X = Math.Min(min.X, v.X);
                    min.Y = Math.Min(min.Y, v.Y);
                    min.Z = Math.Min(min.Z, v.Z);

                    max.X = Math.Max(max.X, v.X);
                    max.Y = Math.Max(max.Y, v.Y);
                    max.Z = Math.Max(max.Z, v.Z);
                }
            }

            nodes[idx].aabb = (min, max);
        }

        private static float AABBArea(Vector3 min, Vector3 max)
        {
            Vector3 vec = max - min;
            return vec.X * vec.Y + vec.X * vec.Z + vec.Y * vec.Z;
        }

        private static float GetAxis(Vector3 v, uint axis)
        {
            if (axis == 0) return v.X;
            if (axis == 1) return v.Y;
            return v.Z;
        }
    }
}
