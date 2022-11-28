using System;
using System.Collections.Generic;
using System.Numerics;

namespace RaytracingWPF
{
    struct Tri
    {
        public int obj;
        public Vector3[] v;
        public Vector3 centroid;
        public Vector3 normal;

        public Tri(int ob, params Vector3[] verts)
        {
            v = verts;
            centroid = (verts[0] + verts[1] + verts[2]) / 3f;
            obj = ob;
            Vector3 dir = Vector3.Cross(verts[1] - verts[0], verts[2] - verts[0]);
            normal = Vector3.Normalize(dir);
        }
    }
}
