using System;
using System.Collections.Generic;
using System.Text;

namespace RaytracingWPF
{
    static class Env
    {
        public static List<Object3D> objects = new List<Object3D>();
        public static List<Emitter> lights = new List<Emitter>();

        public static Camera camera;

        public static void Init(int w, int h) { camera = new Camera(0, 0, -5, w, h, 60); }
        public static void Add(Object3D o) { objects.Add(o); }
        public static void Add(Emitter e) { lights.Add(e); }
        public static void Remove(Object3D o) { objects.Remove(o); }
        public static void Remove(Emitter e) { lights.Remove(e); }
    }
}
