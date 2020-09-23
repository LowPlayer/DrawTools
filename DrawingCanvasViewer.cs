using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace DrawTools
{
    [ContentProperty("DrawingCanvas")]
    [TemplatePart(Name = "Part_ScrollViewer", Type = typeof(ScrollViewer))]
    public sealed class DrawingCanvasViewer : Control
    {
        #region 依赖属性

        public static readonly DependencyProperty DrawingCanvasProperty = DependencyProperty.Register("DrawingCanvas", typeof(DrawingCanvas), typeof(DrawingCanvasViewer));
        /// <summary>
        /// 画板
        /// </summary>
        public DrawingCanvas DrawingCanvas { get => (DrawingCanvas)this.GetValue(DrawingCanvasProperty); set => this.SetValue(DrawingCanvasProperty, value); }

        public static readonly DependencyProperty BackgroundImageProperty = DependencyProperty.Register("BackgroundImage", typeof(BitmapSource), typeof(DrawingCanvasViewer), new PropertyMetadata(OnBackgroundImagePropertyChanged));

        private static void OnBackgroundImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DrawingCanvasViewer)d).EnsureInnerSize();
        }

        /// <summary>
        /// 背景图
        /// </summary>
        public BitmapSource BackgroundImage { get => (BitmapSource)this.GetValue(BackgroundImageProperty); set => this.SetValue(BackgroundImageProperty, value); }

        public static readonly DependencyProperty InnerWidthProperty = DependencyProperty.Register("InnerWidth", typeof(Double), typeof(DrawingCanvasViewer), new PropertyMetadata(Double.NaN));

        public Double InnerWidth { get => (Double)this.GetValue(InnerWidthProperty); set => this.SetValue(InnerWidthProperty, value); }

        public static readonly DependencyProperty InnerHeightProperty = DependencyProperty.Register("InnerHeight", typeof(Double), typeof(DrawingCanvasViewer), new PropertyMetadata(Double.NaN));

        public Double InnerHeight { get => (Double)this.GetValue(InnerHeightProperty); set => this.SetValue(InnerHeightProperty, value); }

        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(Double), typeof(DrawingCanvasViewer), new PropertyMetadata(Double.NaN));

        public Double ImageWidth { get => (Double)this.GetValue(ImageWidthProperty); set => this.SetValue(ImageWidthProperty, value); }

        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(Double), typeof(DrawingCanvasViewer), new PropertyMetadata(Double.NaN));

        public Double ImageHeight { get => (Double)this.GetValue(ImageHeightProperty); set => this.SetValue(ImageHeightProperty, value); }

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(Double), typeof(DrawingCanvasViewer), new PropertyMetadata(Double.NaN, OnZoomPropertyChanged));

        private static void OnZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (DrawingCanvasViewer)d;
            viewer.EnsureInnerSize();
            viewer.EnsureImageSize();
        }

        /// <summary>
        /// 缩放百分比
        /// </summary>
        public Double Zoom { get => (Double)this.GetValue(ZoomProperty); set => this.SetValue(ZoomProperty, value); }

        #endregion

        #region 构造器

        static DrawingCanvasViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingCanvasViewer), new FrameworkPropertyMetadata(typeof(DrawingCanvasViewer)));
        }

        public DrawingCanvasViewer()
        {
            this.Loaded += OnFirstLoaded;
        }

        #endregion

        #region 公开方法

        public void ScrollBy(Double dx, Double dy)
        {
            if (!CanDrag)
                return;

            if (dx != 0)
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + dx);
            if (dy != 0)
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + dy);
        }

        #endregion

        #region 私有方法

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (scrollViewer != null)
                scrollViewer.ScrollChanged -= OnScrollChanged;

            scrollViewer = this.Template.FindName("Part_ScrollViewer", this) as ScrollViewer;

            if (scrollViewer == null)
                throw new NullReferenceException("模板找不到Part_ScrollViewer");
        }

        private void OnFirstLoaded(Object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnFirstLoaded;

            if (BackgroundImage == null || scrollViewer == null)
                return;

            if (Double.IsNaN(this.Zoom))
                EnsureZoom();

            EnsureInnerSize();

            this.SizeChanged += OnSizeChanged;
            OnSizeChanged(sender, e);

            scrollViewer.ScrollChanged += OnScrollChanged;
        }

        private void EnsureZoom()
        {
            if (BackgroundImage == null || scrollViewer == null)
            {
                this.ClearValue(ZoomProperty);
                return;
            }

            var zoom = Math.Min(scrollViewer.ViewportWidth / BackgroundImage.Width, scrollViewer.ViewportHeight / BackgroundImage.Height);
            this.SetCurrentValue(ZoomProperty, Math.Min(1, zoom));
        }

        private void EnsureInnerSize()
        {
            if (BackgroundImage == null || scrollViewer == null || Double.IsNaN(this.Zoom))
            {
                this.ClearValue(InnerWidthProperty);
                this.ClearValue(InnerHeightProperty);
                return;
            }

            var width = BackgroundImage.Width * this.Zoom;
            var height = BackgroundImage.Height * this.Zoom;

            if (width < scrollViewer.ViewportWidth)
                width = scrollViewer.ViewportWidth;

            if (height < scrollViewer.ViewportHeight)
                height = scrollViewer.ViewportHeight;

            scrollViewer.ScrollChanged -= OnScrollChanged;

            this.SetValue(InnerWidthProperty, width);
            this.SetValue(InnerHeightProperty, height);

            scrollViewer.ScrollChanged += OnScrollChanged;

            this.EnsureScrollOffset();
        }

        private void EnsureImageSize()
        {
            if (BackgroundImage == null || Double.IsNaN(this.Zoom))
            {
                this.ClearValue(ImageWidthProperty);
                this.ClearValue(ImageHeightProperty);
                return;
            }

            this.SetValue(ImageWidthProperty, BackgroundImage.Width * this.Zoom);
            this.SetValue(ImageHeightProperty, BackgroundImage.Height * this.Zoom);
        }

        private void EnsureScrollOffset()
        {
            var offsetX = (this.InnerWidth - scrollViewer.ActualWidth) * center.X;
            var offsetY = (this.InnerHeight - scrollViewer.ActualHeight) * center.Y;

            scrollViewer.ScrollToHorizontalOffset(offsetX);
            scrollViewer.ScrollToVerticalOffset(offsetY);
        }

        private void OnSizeChanged(Object sender, RoutedEventArgs e)
        {
            this.EnsureScrollOffset();
        }

        private void OnScrollChanged(Object sender, ScrollChangedEventArgs e)
        {
            if (this.InnerWidth == scrollViewer.ActualWidth)
            {
                center.X = 0.5;
                center.Y = 0.5;
            }
            else
            {
                center.X = scrollViewer.HorizontalOffset / (this.InnerWidth - scrollViewer.ActualWidth);
                center.Y = scrollViewer.VerticalOffset / (this.InnerHeight - scrollViewer.ActualHeight);
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Zoom *= 1 + (e.Delta > 0 ? scale : -scale);
                e.Handled = true;
            }
        }

        #endregion

        #region 属性

        public Boolean CanDrag => this.scrollViewer != null && (InnerHeight > scrollViewer.ViewportHeight || InnerWidth > scrollViewer.ViewportWidth);

        #endregion

        #region 字段

        private ScrollViewer scrollViewer;
        private Point center = new Point(0.5, 0.5);
        private Double scale = 0.05;

        #endregion
    }
}
