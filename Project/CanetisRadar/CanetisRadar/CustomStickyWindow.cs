using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanetisRadar
{
    /// <summary>
    /// Represents a StickyWindow which uses a GraphicsWindow sticks to a parent window.
    /// </summary>
    public class CustomStickyWindow : GraphicsWindow
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// Gets or sets a Boolean which indicates wether to stick to the parents client area.
        /// </summary>
        public bool AttachToClientArea { get; set; }

        /// <summary>
        /// Gets or sets a Boolean which indicates wether to bypass the need of the windows Topmost flag.
        /// </summary>
        public bool BypassTopmost { get; set; }

        /// <summary>
        /// Gets or Sets an IntPtr which is used to identify the parent window.
        /// </summary>
        public IntPtr ParentWindowHandle { get; set; }

        /// <summary>
        /// Initializes a new StickyWindow with a default window position and size.
        /// </summary>
        public CustomStickyWindow()
        {
            X = 0;
            Y = 0;
            Width = 800;
            Height = 600;
        }

        /// <summary>
        /// Initializes a new StickyWindow with the given window position and size.
        /// </summary>
        /// <param name="x">The position of the window on the X-Axis of the desktop.</param>
        /// <param name="y">The position of the window on the Y-Axis of the desktop.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public CustomStickyWindow(int x, int y, int width, int height) : base(x, y, width, height)
        {
        }

        /// <summary>
        /// Initializes a new StickyWindow with the given window position and size.
        /// </summary>
        /// <param name="x">The position of the window on the X-Axis of the desktop.</param>
        /// <param name="y">The position of the window on the Y-Axis of the desktop.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="device">Optionally specify a Graphics device to use.</param>
        public CustomStickyWindow(int x, int y, int width, int height, Graphics device = null) : base(x, y, width,
            height, device)
        {
        }

        /// <summary>
        /// Initializes a new StickyWindow with the given window position and size and the window handle of the parent window.
        /// </summary>
        /// <param name="x">The position of the window on the X-Axis of the desktop.</param>
        /// <param name="y">The position of the window on the Y-Axis of the desktop.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="parentWindow">An IntPtr representing the parent windows handle.</param>
        /// <param name="device">Optionally specify a Graphics device to use.</param>
        public CustomStickyWindow(int x, int y, int width, int height, IntPtr parentWindow, Graphics device = null) :
            base(x, y, width, height, device)
        {
            if (!IsWindow(parentWindow)) throw new ArgumentOutOfRangeException(nameof(parentWindow));

            ParentWindowHandle = parentWindow;
        }

        /// <summary>
        /// Initializes a new StickyWindow with the ability to stick to a parent window.
        /// </summary>
        /// <param name="parentWindow">An IntPtr representing the parent windows handle.</param>
        /// <param name="device">Optionally specify a Graphics device to use.</param>
        public CustomStickyWindow(IntPtr parentWindow, Graphics device = null) : base(device)
        {
            if (!IsWindow(parentWindow)) throw new ArgumentOutOfRangeException(nameof(parentWindow));

            ParentWindowHandle = parentWindow;
        }

        /// <summary>
        /// Gets called when the timer thread needs to render a new Scene / frame.
        /// </summary>
        protected override void OnDrawGraphics(int frameCount, long frameTime, long deltaTime)
        {
            var elapsedTime = frameCount * deltaTime;

            if (elapsedTime > 34)
            {
                // executes 30 times per second
                if (ParentWindowHandle != IntPtr.Zero)
                {
                    if (BypassTopmost) PlaceAbove(ParentWindowHandle);

                    FitTo(ParentWindowHandle, AttachToClientArea);
                }
                else
                {
                    _updateParentWindowHandleThread = new Thread(delegate()
                    {
                        while (true)
                        {
                            ParentWindowHandle = GetForegroundWindow();

                            Console.WriteLine(ParentWindowHandle);

                            if (BypassTopmost) PlaceAbove(ParentWindowHandle);

                            FitTo(ParentWindowHandle, AttachToClientArea);

                            Thread.Sleep(1000);
                        }
                    });
                    _updateParentWindowHandleThread.Start();
                }
            }

            base.OnDrawGraphics(frameCount, frameTime, deltaTime);
        }

        private Thread _updateParentWindowHandleThread;

        ~CustomStickyWindow()
        {
            if (_updateParentWindowHandleThread != null)
            {
                _updateParentWindowHandleThread.Abort();
            }
        }
    }
}