﻿using System;
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
                foreach (Face f in Env.objects[i].faces)
                {
                    t.Add(new Tri(i, Env.objects[i].WorldSpace(f)));
                    n++;
                }
            }

            if (n < 1) return false;
            tris = t.ToArray();
            nodes = new BVHNode[2 * n - 1];

            //calculate centroids
            for (int i = 0; i < n; i++) tris[i].centroid = (tris[i].v[0] + tris[i].v[1] + tris[i].v[2]) / 3f;

            uint idx = 0;
            nodes[idx].childPtr = 0;
            nodes[idx].triOffset = 0;
            nodes[idx].triCount = n;
            AABB(idx);

            Subdivide(idx);

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
            for (uint x = 0; x < node.triCount; x++)
            {
                Tri t = tris[node.childPtr + x];
                for (uint axis = 0; axis < 3; axis++)
                {
                    float pos = GetAxis(t.centroid, axis);
                    float cost = SAHeuristic(idx, pos, axis);
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

        private static float SAHeuristic(uint idx, float pos, uint axis)
        {
            uint i = 0;
            uint j = 0;

            (Vector3 lMin, Vector3 lMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            (Vector3 rMin, Vector3 rMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));

            for (uint x = 0; x < nodes[idx].triCount; x++)
            {
                Tri t = tris[nodes[idx].childPtr + x];
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
            for (uint i = nodes[idx].triOffset; i < nodes[idx].triOffset + nodes[idx].triCount; i++)
            {
                Vector3[] verts = tris[i].v;

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