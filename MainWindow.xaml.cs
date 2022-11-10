using System;
using System.IO;
using System.Threading;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Input;
using System.ComponentModel;
using Microsoft.Win32;

namespace RaytracingWPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts = new CancellationTokenSource();
        private WriteableBitmap frame;

        private Emitter fillLight;

        public MainWindow()
        {
            InitializeComponent();

            int w = (int)image.Width;
            int h = (int)image.Height;

            Env.Init(w, h);
            foreach (Object3D o in Env.objects)
                listBox.Items.Add(o.color);

            BVH.Build();

            Vector3 t = Env.camera.transform.Translation;
            fillLight = new Emitter(t.X, t.Y, t.Z, Vector3.One, 1);
            Env.Add(fillLight);

            frame = new WriteableBitmap(w, h, 96d, 96d, PixelFormats.Rgb24, null);
            image.Source = frame;

            Thread drawThread = new Thread(new ThreadStart(() => Update(cts.Token)));
            drawThread.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            cts.Cancel();
            base.OnClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Vector4 fwd = Env.camera.ApplyTransform(new Vector4(0, 0, 1, 0));
            Vector3 forward = new Vector3(fwd.X, fwd.Y, fwd.Z);
            if (e.Key == Key.Left) Env.camera.transform *= Matrix4x4.CreateRotationY(-0.1f);
            else if (e.Key == Key.Right) Env.camera.transform *= Matrix4x4.CreateRotationY(0.1f);
            else if (e.Key == Key.Down) Env.camera.transform *= Matrix4x4.CreateFromAxisAngle(Vector3.Cross(forward, new Vector3(0, -1, 0)), -0.05f);
            else if (e.Key == Key.Up) Env.camera.transform *= Matrix4x4.CreateFromAxisAngle(Vector3.Cross(forward, new Vector3(0, -1, 0)), 0.05f);
            fillLight.transform = Env.camera.transform;
            base.OnKeyDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            Env.camera.transform *= Matrix4x4.CreateScale(1 + (e.Delta * -0.001f));
            fillLight.transform = Env.camera.transform;
            base.OnMouseWheel(e);
        }

        private void Update(CancellationToken token)
        {
            byte[] data;
            while (!token.IsCancellationRequested)
            {
                data = Env.camera.CreateImage();
                if (!token.IsCancellationRequested)
                    Dispatcher.Invoke(() => frame.WritePixels(new Int32Rect(0, 0, frame.PixelWidth, frame.PixelHeight), data, frame.PixelWidth * 3, 0));
            }
        }

        private void ImportButton(object sender, RoutedEventArgs e)
        {
            string[] pos = importPositionArgs.Text.Split(',');
            string[] mat = importMaterialArgs.Text.Split(',');

            if (pos.Length != 3 || mat.Length != 3) return;

            float[] parsed;
            try
            {
                parsed = new float[] {
                    float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]),
                    float.Parse(mat[0]), float.Parse(mat[1]), float.Parse(mat[2])
                };
            }
            catch (FormatException) { return; }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Title = "Open .obj";
            ofd.Filter = "3D Objects (*.obj)|*.obj";
            ofd.ShowDialog();

            if (ObjReader.ReadFile(ofd.FileName, out Object3D obj))
            {
                obj.transform.Translation = new Vector3(parsed[1], parsed[2], parsed[3]);
                obj.SetProperties(parsed[3], parsed[4], parsed[5]);
                Env.Add(obj);
                listBox.Items.Add(obj.color);
                BVH.Build();
            }
        }

        private void UpdateButton(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedIndex == 1) return;

            string[] pos = positionArgs.Text.Split(',');
            string[] mat = materialArgs.Text.Split(',');
            Object3D obj = Env.objects[listBox.SelectedIndex];

            if (pos.Length != 6 || mat.Length != 3 || obj == null) return;

            float[] parsed;
            try
            {
                parsed = new float[] {
                    float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]),
                    float.Parse(pos[3]), float.Parse(pos[4]), float.Parse(pos[5]),
                    float.Parse(mat[0]), float.Parse(mat[1]), float.Parse(mat[2])
                };
            }
            catch (FormatException) { return; }

            obj.transform = Matrix4x4.CreateFromYawPitchRoll(parsed[3], parsed[4], parsed[5]);
            obj.transform.Translation = new Vector3(parsed[0], parsed[1], parsed[2]);
            obj.SetProperties(parsed[6], parsed[7], parsed[8]);

            BVH.Build();
        }
    }
}
