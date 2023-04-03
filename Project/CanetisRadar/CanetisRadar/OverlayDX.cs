using GameOverlay.Drawing;
using GameOverlay.Windows;
using IniParser;
using IniParser.Model;
using NAudio.CoreAudioApi;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Image = GameOverlay.Drawing.Image;

namespace CanetisRadar
{
    public class OverlayDX : IDisposable
    {
        // -------------------------------------------------------
        // Variables
        // -------------------------------------------------------
        private MMDeviceEnumerator enumerator;

        private MMDevice device;

        private int multiplier = 100;
        private bool showInfoText = false;

        private readonly CustomStickyWindow _window;

        private readonly Dictionary<string, SolidBrush> _brushes;
        private readonly Dictionary<string, Font> _fonts;
        private readonly Dictionary<string, Image> _images;

        private System.Drawing.Rectangle screenRectangle;

        public OverlayDX()
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(AppDomain.CurrentDomain.BaseDirectory + "settings.ini");
            string m = data["basic"]["multiplier"];
            multiplier = Int32.Parse(m);
            string s = data["basic"]["showInfoText"];
            showInfoText = Boolean.Parse(s);

            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();
            _images = new Dictionary<string, Image>();

            screenRectangle = new System.Drawing.Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);

            var gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new CustomStickyWindow(0, 0, (int)screenRectangle.Width, (int)screenRectangle.Height, gfx)
            {
                FPS = 10,
                IsTopmost = true, // make the window always on top
                IsVisible = true, // make the window visible
                AttachToClientArea = false,
            };

            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.SetupGraphics += _window_SetupGraphics;
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
                foreach (var pair in _images) pair.Value.Dispose();
            }

            _brushes["transparent"] = gfx.CreateSolidBrush(0, 0, 0, 0);
            _brushes["black50"] = gfx.CreateSolidBrush(0, 0, 0, (float)255 / 2);

            _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
            _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
            _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
            _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
            _brushes["blue"] = gfx.CreateSolidBrush(0, 0, 255);
            _brushes["background"] = gfx.CreateSolidBrush(0x33, 0x36, 0x3F);
            _brushes["grid"] = gfx.CreateSolidBrush(255, 255, 255, 0.2f);
            _brushes["random"] = gfx.CreateSolidBrush(0, 0, 0);

            if (e.RecreateResources) return;

            _fonts["arial"] = gfx.CreateFont("Arial", 12);
            _fonts["consolas"] = gfx.CreateFont("Consolas", 14);
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();
            foreach (var pair in _images) pair.Value.Dispose();
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            gfx.ClearScene(_brushes["transparent"]);

            DrawInfoTextAndRadar(gfx);
        }

        public void Run()
        {
            new OverlayNotifyIcon().Init();
            _window.Create();
            // Shit here
            new Thread(delegate() { _window.Join(); }).Start();
        }

        // -------------------------------------------------------
        // Main Loop
        // -------------------------------------------------------
        public void DrawInfoTextAndRadar(Graphics gfx)
        {
            enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            if (device.AudioMeterInformation.PeakValues.Count < 8)
            {
                MessageBox.Show("您没有使用 7.1 音频设备！请再次查看设置指南。", "未检测到 7.1 音频！", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            while (true)
            {
                float lefttop = device.AudioMeterInformation.PeakValues[0];
                float righttop = device.AudioMeterInformation.PeakValues[1];
                float leftbottom = device.AudioMeterInformation.PeakValues[4];
                float rightbottom = device.AudioMeterInformation.PeakValues[5];

                var tempone = lefttop * multiplier;
                var temptwo = righttop * multiplier;

                var tempthree = leftbottom * multiplier;
                var tempfour = rightbottom * multiplier;

                var x = 75 - tempone + temptwo;
                var y = 75 - tempone - temptwo;

                x = x - tempthree + tempfour;
                y = y + tempthree + tempfour;

                if (y < 10)
                {
                    y = 10;
                }

                if (x < 10)
                {
                    x = 10;
                }

                if (y > 140)
                {
                    y = 140;
                }

                if (x > 140)
                {
                    x = 140;
                }

                string infoText = "";
                for (int i = 0; i < device.AudioMeterInformation.PeakValues.Count; i++)
                {
                    infoText += i + " -> " + device.AudioMeterInformation.PeakValues[i] +
                                (i != device.AudioMeterInformation.PeakValues.Count - 1 ? "\n" : "");
                }

                gfx.BeginScene();

                gfx.ClearScene();

                if (showInfoText)
                {
                    gfx.DrawTextWithBackground(_fonts["consolas"], _brushes["green"], _brushes["black50"], 58, 20,
                        infoText);
                }

                var radarBitmap = CreateRadar((int)x, (int)y);

                var radarBitmapBytes = BitmapToByteArray(radarBitmap);

                gfx.DrawImage(new Image(gfx, radarBitmapBytes), screenRectangle.Width / 2 - (radarBitmap.Width / 2),
                    screenRectangle.Height / 2, 0.5f);

                gfx.EndScene();
            }
        }

        public System.Drawing.Bitmap CreateRadar(int x, int y)
        {
            System.Drawing.Bitmap radar = new System.Drawing.Bitmap(150, 150);
            System.Drawing.Graphics grp = System.Drawing.Graphics.FromImage(radar);
            grp.FillEllipse(System.Drawing.Brushes.Black, 0, 0, radar.Width, radar.Height);

            grp.FillEllipse(System.Drawing.Brushes.Red, x - 5, y - 5, 10, 10);

            return radar;
        }

        public static byte[] BitmapToByteArray(System.Drawing.Bitmap bitmap)
        {
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Bmp);
            return stream.ToArray();
        }


        ~OverlayDX()
        {
            Dispose(false);
        }

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _window.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}