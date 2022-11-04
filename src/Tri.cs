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

        public Tri(int ob, params Vector3[] verts)
        {
            v = verts;
            centroid = new Vector3();
            obj = ob;
        }
    }

    struct Face
    {
        public uint[] vIdx;
        public Vector3 normal;

        public Face(Object3D obj, uint[] v)
        {
            vIdx = v;
            Vector3 dir = Vector3.Cross(obj.verts[v[1]] - obj.verts[v[0]], obj.verts[v[2]] - obj.verts[v[0]]);
            normal = Vector3.Normalize(dir);
        }
    }
}
