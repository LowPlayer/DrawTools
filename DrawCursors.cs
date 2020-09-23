using Microsoft.Win32.SafeHandles;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DrawTools
{
    public static class DrawCursors
    {
        public static Cursor CreateBmpCursor(Bitmap bmp, UInt32 xHotSpot, UInt32 yHotSpot)
        {
            if (!NativeMethods.GetIconInfo(bmp.GetHicon(), out IconInfo iconInfo))
                return Cursors.Arrow;

            iconInfo.xHotspot = xHotSpot;   // 焦点x轴坐标
            iconInfo.yHotspot = yHotSpot;   // 焦点y轴坐标
            iconInfo.fIcon = false;         // 设置鼠标

            var cursorHandle = NativeMethods.CreateIconIndirect(ref iconInfo);
            return CursorInteropHelper.Create(cursorHandle);
        }

        public static Cursor CreateBmpCursor(Uri uri, UInt32 xHotSpot, UInt32 yHotSpot)
        {
            var sri = Application.GetResourceStream(uri);

            using (var bmp = new Bitmap(sri.Stream))
            {
                return CreateBmpCursor(bmp, xHotSpot, yHotSpot);
            }
        }

        public static Cursor CreateBmpCursor(UInt32 width, UInt32 height, UInt32 border, System.Windows.Media.SolidColorBrush brush)
        {
            using (var bmp = new Bitmap((Int32)(width + border * 2), (Int32)(height + border * 2)))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    var color = brush.Color;
                    using (var pen = new Pen(Color.FromArgb(color.A, color.R, color.G, color.B), border))
                    {
                        g.DrawRectangle(pen, border, border, width, height);
                        g.Flush();
                    }
                }

                return CreateBmpCursor(bmp, width / 2 + border, height / 2 + border);
            }
        }

        private static Lazy<Cursor> hand = new Lazy<Cursor>(() => { return CreateBmpCursor(new Uri("Images/Cursor/hand.png", UriKind.Relative), 12, 12); });
        /// <summary>
        /// 拖动
        /// </summary>
        public static Cursor Hand => hand.Value;
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern SafeIconHandle CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        public static extern Boolean DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GetIconInfo(IntPtr hIcon, out IconInfo pIconInfo);
    }

    public struct IconInfo
    {
        public Boolean fIcon;
        public UInt32 xHotspot;
        public UInt32 yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeIconHandle() : base(true) { }
        protected override Boolean ReleaseHandle() => NativeMethods.DestroyIcon(handle);
    }
}
