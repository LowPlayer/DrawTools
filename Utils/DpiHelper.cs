using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace DrawTools.Utils
{
    public struct Dpi
    {
        public Dpi(Double x, Double y)
        {
            DpiX = x;
            DpiY = y;

            Px2WpfX = PxToWpf(1, DpiX);
            Px2WpfY = PxToWpf(1, DpiY);
            Cm2WpfX = CmToPx(1, DpiX) * Px2WpfX;
            Cm2Wpfy = CmToPx(1, DpiY) * Px2WpfY;
            Pt2Wpfx = PtToPx(1, DpiX) * Px2WpfX;
            Pt2Wpfy = PtToPx(1, DpiY) * Px2WpfY;
            In2WpfX = InToPx(1, DpiX) * Px2WpfX;
            In2WpfY = InToPx(1, DpiY) * Px2WpfY;
        }

        #region 方法

        /// <summary>
        /// 厘米转像素
        /// </summary>
        public static Double CmToPx(Double length, Double dpi)
        {
            return length * dpi / 2.54;
        }

        public static Double PtToPx(Double length, Double dpi)
        {
            return length * dpi / 72;
        }

        public static Double WpfToPx(Double length, Double dpi)
        {
            return length / 96 * dpi;
        }

        public static Double PxToCm(Double length, Double dpi)
        {
            return length * 2.54 / dpi;
        }

        public static Double PxToPt(Double length, Double dpi)
        {
            return length * 72 / dpi;
        }

        public static Double PxToWpf(Double length, Double dpi)
        {
            return length / dpi * 96;
        }

        public static Double InToPx(Double length, Double dpi)
        {
            return length * dpi;
        }

        #endregion

        public Double DpiX { get; }

        public Double DpiY { get; }

        public Double Px2WpfX { get; }

        public Double Px2WpfY { get; }

        public Double Cm2WpfX { get; }

        public Double Cm2Wpfy { get; }

        public Double Pt2Wpfx { get; }

        public Double Pt2Wpfy { get; }

        public Double In2WpfX { get; }

        public Double In2WpfY { get; }

        /// <summary>
        /// 英寸-厘米
        /// </summary>
        public static readonly Double In2Cm = 2.54;
        /// <summary>
        /// 英寸-磅
        /// </summary>
        public static readonly Double In2Pt = 72;
    }

    public sealed class DpiHelper
    {
        static DpiHelper()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpi = new Dpi(graphics.DpiX, graphics.DpiY);
            }
        }

        #region 方法

        public static Dpi GetDpiFromVisual(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            return (source == null || source.CompositionTarget == null) ? dpi : new Dpi(96.0 * source.CompositionTarget.TransformToDevice.M11, 96.0 * source.CompositionTarget.TransformToDevice.M22);
        }

        #endregion

        #region 字段

        public static readonly Dpi dpi;

        #endregion
    }
}
