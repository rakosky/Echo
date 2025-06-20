using Echo.Extern;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Echo.Services
{
    public class ScreenshotProvider
    {
        TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(250);

        Bitmap? _image = null;
        BitmapData? _imageBits = null;
        ReadWriteLock _captureLock = new ReadWriteLock();

        readonly ILogger<ScreenshotProvider> _logger;
        readonly ProcessProvider _processProvider;

        public ScreenshotProvider(ILogger<ScreenshotProvider> logger, ProcessProvider processProvider)
        {
            _captureLock.AcquireReadLock();// Start in locked state so we don't return null images

            _logger = logger;
            _processProvider = processProvider;
        }

        /// <summary>
        /// Provides the latest screenshot of the game window.
        /// DO NOT DISPOSE THIS IMAGE.
        /// </summary>
        /// <returns></returns>
        public async Task<Bitmap> GetLatestScreenshot()
        {
            await _captureLock.WaitForReadLockReleaseAsync();
            return _image;
        }

        public async Task<PixelSnapshot> GetLatestPixels()
        {
            await _captureLock.WaitForReadLockReleaseAsync();

            int bytes = Math.Abs(_imageBits.Stride) * _imageBits.Height;
            byte[] buffer = new byte[bytes];

            Marshal.Copy(_imageBits.Scan0, buffer, 0, bytes);
            var snapshot = new PixelSnapshot(buffer, _imageBits.Width, _imageBits.Height, _imageBits.Stride);
            
            //Save(snapshot, $@"{AppDomain.CurrentDomain.BaseDirectory}screenshots\{DateTime.Now.Ticks}.png");
            return snapshot;
        }

        public async Task StartScreenCaptureLoop()
        {
            while (true)
            {
                try
                {
                    _captureLock.AcquireReadLock();
                    Bitmap? newImage = null;
                    BitmapData? newImageBits = null;

                    var gameProc = _processProvider.TryGetProcess();

                    if (gameProc is null || gameProc.MainWindowHandle == IntPtr.Zero)
                    {
                        await Task.Delay(_refreshInterval);
                        continue;
                    }

                    newImage = CaptureWithPrintWindow(gameProc.MainWindowHandle);
                    //newImage = CaptureWindowInternal(gameProc.MainWindowHandle);
                    newImageBits = newImage.LockBits(
                        new Rectangle(0, 0, newImage.Width, newImage.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format24bppRgb);


                    // Clean up old image AFTER acquiring lock and BEFORE swapping
                    if (_image is not null && _imageBits is not null)
                    {
                        _image.UnlockBits(_imageBits);
                        _image.Dispose();
                    }

                    _image = newImage;
                    _imageBits = newImageBits;

                    //_image.Save($@"{AppDomain.CurrentDomain.BaseDirectory}screenshots\{DateTime.Now.Ticks}.png");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error capturing screenshot: {Message}", ex.Message);
                }
                finally
                {
                    _captureLock.ReleaseReadLock();
                }

                await Task.Delay(_refreshInterval);
            }
        }

        /// <summary>
        /// Saves a PixelSnapshot to <paramref name="filePath"/> (PNG by default).
        /// Assumes the buffer is 24-bpp B-G-R.
        /// </summary>
        public static void Save(PixelSnapshot snap, string filePath,
                                ImageFormat? format = null)
        {
            format ??= ImageFormat.Png;      // default to PNG

            // 1) create a blank 24-bpp bitmap
            using var bmp = new Bitmap(snap.Width, snap.Height,
                                       PixelFormat.Format24bppRgb);

            // 2) lock it for writing
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, snap.Width, snap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int srcStride = snap.Stride;
                int destStride = bmpData.Stride;
                int bytesPerRow = Math.Abs(srcStride);

                // 3) copy row-by-row (handles stride differences & upside-down data)
                unsafe
                {
                    fixed (byte* pSrcBase = snap.Buffer)
                    {
                        byte* pDestBase = (byte*)bmpData.Scan0;

                        for (int y = 0; y < snap.Height; y++)
                        {
                            int srcRow = srcStride < 0
                                           ? (snap.Height - 1 - y) * bytesPerRow   // bottom-up DIB
                                           : y * bytesPerRow;                       // top-down
                            byte* pSrc = pSrcBase + srcRow;
                            byte* pDest = pDestBase + y * destStride;

                            Buffer.MemoryCopy(pSrc, pDest, bytesPerRow, bytesPerRow);
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            // 4) save to disk
            bmp.Save(filePath, format);
        }

        private const uint PW_RENDERFULLCONTENT = 0x00000002;

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int left, top, right, bottom; }

        public static Bitmap CaptureWithPrintWindow(IntPtr hwnd)
        {
            if (!GetWindowRect(hwnd, out var r))
                throw new System.ComponentModel.Win32Exception();

            int w = r.right - r.left, h = r.bottom - r.top;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                // try full‐content flag
                PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT);
                g.ReleaseHdc(hdc);
            }

            return bmp;
        }

        private Bitmap CaptureWindowInternal(nint handle)
        {
            // Get the hDC of the target window
            nint hdcSrc = User32.GetWindowDC(handle);
            // Get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);

            // Get the size of the client area (the window content)
            var clientRect = GetClientAreaRectangle(handle);
            // Create a device context we can copy to
            nint hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // Create a bitmap we can copy to, using GetDeviceCaps to get the width/height
            nint hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, clientRect.Width, clientRect.Height);
            // Select the bitmap object
            nint hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // BitBlt over
            GDI32.BitBlt(hdcDest, 0, 0, clientRect.Width, clientRect.Height, hdcSrc, clientRect.X - windowRect.left, clientRect.Y - windowRect.top, GDI32.SRCCOPY);
            // Restore selection
            GDI32.SelectObject(hdcDest, hOld);

            // Get a .NET image object for it
            var img = Image.FromHbitmap(hBitmap);
            // Clean up
            GDI32.DeleteObject(hBitmap); // Release the bitmap resource
            GDI32.DeleteDC(hdcDest); // Release the device context
            User32.ReleaseDC(handle, hdcSrc); // Release the device context

            return img;
        }
        private Rectangle GetClientAreaRectangle(nint handle)
        {
            User32.RECT clientRect = new User32.RECT();
            User32.GetClientRect(handle, ref clientRect);

            User32.POINT upperLeft = new User32.POINT { x = clientRect.left, y = clientRect.top };
            User32.ClientToScreen(handle, ref upperLeft);

            User32.POINT lowerRight = new User32.POINT { x = clientRect.right, y = clientRect.bottom };
            User32.ClientToScreen(handle, ref lowerRight);

            return new Rectangle(upperLeft.x, upperLeft.y, lowerRight.x - upperLeft.x, lowerRight.y - upperLeft.y);
        }

    }

    public readonly record struct PixelSnapshot(
    byte[] Buffer,          // raw pixel bytes
    int Width,
    int Height,
    int Stride);         // bytes per scan line
}
