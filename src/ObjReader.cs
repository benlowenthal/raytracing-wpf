using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows.Media;

namespace RaytracingWPF
{
    class ObjReader
    {
        public static bool ReadFile(string path, out Object3D obj)
        {
            obj = new Object3D(0, 0, 0);
            if (!File.Exists(path)) return false;

            using StreamReader sr = new StreamReader(path);

            List<uint[]> faces = new List<uint[]>();
            List<Vector3> verts = new List<Vector3>();

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] vals = line.Split(' ');
                if (vals[0] == "v")
                    verts.Add(new Vector3(float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3])));
                if (vals[0] == "f")
                    faces.Add(new uint[] { uint.Parse(vals[1]) - 1, uint.Parse(vals[2]) - 1, uint.Parse(vals[3]) - 1 });
            }

            obj = new Object3D(0, 0, 0, verts.ToArray(), faces.ToArray(), Colors.Red, 0, 0.75f);
            return true;
        }
    }
}
