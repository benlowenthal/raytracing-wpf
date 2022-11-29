using System;
using System.Collections.Generic;
using System.Numerics;

namespace RaytracingWPF
{
    struct Tri
    {
        public int obj;
        public uint[] uv;
        public Vector3[] v;
        public Vector3 centroid;
        public Vector3 normal;

        public Tri(int ob, uint[] uvs, params Vector3[] verts)
        {
            v = verts;
            uv = uvs;
            obj = ob;
            centroid = (verts[0] + verts[1] + verts[2]) / 3f;
            normal = Vector3.Normalize(Vector3.Cross(verts[1] - verts[0], verts[2] - verts[0]));
        }
    }
}
