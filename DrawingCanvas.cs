using DrawTools.Serialize;
using DrawTools.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace DrawTools
{
    public sealed class DrawingCanvas : Canvas
    {
        public DrawingCanvas()
        {
            this.ClipToBounds = true;
            this.Focusable = true;
            this.OriginalCursor = Cursors.Arrow;
            this.squareCursor = false;

            this.Pen = new Pen(this.Brush, this.StrokeThickness);
            this.SelectBackgroundPen = new Pen(Brushes.White, 1);
            this.SelectPen = new Pen(Brushes.Black, 1) { DashStyle = new DashStyle(new Double[] { 4 }, 0) };

            this.Loaded += delegate
            {
                this.Focus();
                this.drawViewer = this.FindParent<DrawingCanvasViewer>();
            };
        }

        #region 可视化

        private List<Visual> visuals = new List<Visual>();

        protected override Int32 VisualChildrenCount => visuals.Count + Children.Count;

        protected override Visual GetVisualChild(Int32 index)
        {
            if (index < visuals.Count)
                return visuals[index];
            else
                return Children[index - visuals.Count];
        }

        public void AddVisual(Visual visual)
        {
            visuals.Add(visual);

            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        public void Insert(Int32 index, Visual visual)
        {
            visuals.Insert(index, visual);

            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        public void MovoToHead(Visual visual)
        {
            var index = visuals.IndexOf(visual);

            if (index <= 0)
                return;

            visuals.RemoveAt(index);
            visuals.Insert(0, visual);
        }

        public void DeleteVisual(Visual visual)
        {
            visuals.Remove(visual);

            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }

        public DrawingVisual GetVisual(Point point)
        {
            var hitResult = VisualTreeHelper.HitTest(this, point);
            return hitResult.VisualHit as DrawingVisual;
        }

        #endregion

        #region 依赖属性

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(SolidColorBrush), typeof(DrawingCanvas), new PropertyMetadata(Brushes.Black, OnBrushPropertyChanged));

        private static void OnBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawing = (DrawingCanvas)d;
            drawing.Pen.Brush = (SolidColorBrush)e.NewValue;
            drawing.UpdateCursor();
        }

        /// <summary>
        /// 画刷颜色
        /// </summary>
        public SolidColorBrush Brush { get => (SolidColorBrush)this.GetValue(BrushProperty); set => this.SetValue(BrushProperty, value); }

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(UInt32), typeof(DrawingCanvas), new PropertyMetadata(1u, OnStrokeThicknessPropertyChanged));

        private static void OnStrokeThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawing = (DrawingCanvas)d;
            drawing.Pen.Thickness = (UInt32)e.NewValue;
            drawing.UpdateCursor();
        }

        /// <summary>
        /// 画刷宽度
        /// </summary>
        public UInt32 StrokeThickness { get => (UInt32)this.GetValue(StrokeThicknessProperty); set => this.SetValue(StrokeThicknessProperty, value); }

        public static readonly DependencyProperty DrawingToolTypeProperty = DependencyProperty.Register("DrawingToolType", typeof(DrawToolType), typeof(DrawingCanvas), new PropertyMetadata(DrawToolType.Pointer, OnDrawingToolTypePropertyChanged));

        private static void OnDrawingToolTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawing = (DrawingCanvas)d;

            if (!drawing.IsKeyboardFocused)
                drawing.Focus();

            drawing.UpdateCursor();
        }

        /// <summary>
        /// 当前的画图工具
        /// </summary>
        public DrawToolType DrawingToolType { get => (DrawToolType)this.GetValue(DrawingToolTypeProperty); set => this.SetValue(DrawingToolTypeProperty, value); }

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(DrawingCanvas), new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.Inherits));
        /// <summary>
        /// 文本大小
        /// </summary>
        public Double FontSize { get => (Double)this.GetValue(FontSizeProperty); set => this.SetValue(FontSizeProperty, value); }

        public static readonly DependencyProperty ZoomProperty = DrawingCanvasViewer.ZoomProperty.AddOwner(typeof(DrawingCanvas), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(OnZoomPropertyChanged)));

        private static void OnZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DrawingCanvas)d).UpdateCursor();
        }

        /// <summary>
        /// X轴缩放
        /// </summary>
        public Double Zoom { get => (Double)this.GetValue(ZoomProperty); set => this.SetValue(ZoomProperty, value); }

        #endregion

        #region 鼠标键盘事件

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.TouchId == 0 && tool.CanTouchEnter && (e.Handled = tool.OnTouchEnter(e.GetPosition(this))))
                    return;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            lastPoint = null;

            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.TouchId == 0 && tool.CanTouchLeave && (e.Handled = tool.OnTouchLeave(e.GetPosition(this))))
                    return;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!this.IsKeyboardFocused)
                this.Focus();

            if (this.canDragStart)
            {
                lastPoint = e.GetPosition(this.drawViewer);
                e.Handled = true;
                return;
            }

            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.CanTouchDown && (e.Handled = tool.OnTouchDown(0, e.GetPosition(this))))
                    return;
            }

            tool = CreateDrawingTool();

            if (tool.CanTouchDown && (e.Handled = tool.OnTouchDown(0, e.GetPosition(this))))
                return;

            if (this.canDragStart)
            {
                lastPoint = e.GetPosition(this);
                e.Handled = true;
                return;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.canDragMove)
            {
                var drawPoint = e.GetPosition(this.drawViewer);
                var dx = lastPoint.Value.X - drawPoint.X;
                var dy = lastPoint.Value.Y - drawPoint.Y;
                this.drawViewer.ScrollBy(dx, dy);
                lastPoint = drawPoint;
                e.Handled = true;
                return;
            }

            var point = e.GetPosition(this);

            if (point.X < 0 || point.Y < 0 || point.X > this.ActualWidth || point.Y > this.ActualHeight)
                return;

            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.TouchId == 0 && tool.CanTouchMove && (e.Handled = tool.OnTouchMove(e.GetPosition(this))))
                    return;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            lastPoint = null;

            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.TouchId == 0 && tool.CanTouchUp && (e.Handled = tool.OnTouchUp(e.GetPosition(this))))
                    return;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.CanKeyDown && (e.Handled = tool.OnKeyDown(e.Key)))
                    return;
            }

            if (e.Key == Key.Space)
            {
                this.OriginalCursor = DrawCursors.Hand;
                this.squareCursor = false;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            IDrawTool tool;

            for (var i = workingDrawTools.Count - 1; i >= 0; i--)
            {
                tool = workingDrawTools[i];

                if (tool.CanKeyUp && (e.Handled = tool.OnKeyUp(e.Key)))
                    return;
            }

            if (e.Key == Key.Space)
                this.UpdateCursor();
        }

        #endregion  

        #region 公开方法

        public void Clear()
        {
            foreach (var visual in visuals)
            {
                base.RemoveLogicalChild(visual);
                base.RemoveVisualChild(visual);
            }

            this.visuals.Clear();
            this.Children.Clear();
            this.workingDrawTools.Clear();
        }

        public IEnumerable<DrawGeometryBase> GetDrawGeometries()
        {
            return visuals.OfType<DrawGeometryBase>().Where(q => q.IsFinish);
        }

        public void AddWorkingDrawTool(IDrawTool drawTool)
        {
            this.workingDrawTools.Add(drawTool);
        }

        public void DeleteWorkingDrawTool(IDrawTool drawTool)
        {
            this.workingDrawTools.Remove(drawTool);
        }

        /// <summary>
        /// 转为图片
        /// </summary>
        /// <param name="pixelWidth"></param>
        /// <param name="pixelHeight"></param>
        /// <param name="dpi"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public BitmapFrame ToBitmapFrame(Int32 pixelWidth, Int32 pixelHeight, Dpi dpi, ImageSource image = null)
        {
            var visual = GetDrawingVisual(pixelWidth, pixelHeight, dpi, image);

            if (visual == null)
                return null;

            var renderBitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi.DpiX, dpi.DpiY, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            return BitmapFrame.Create(renderBitmap);
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="pixelWidth"></param>
        /// <param name="pixelHeight"></param>
        /// <param name="dpi"></param>
        /// <param name="image"></param>
        public void Print(Int32 pixelWidth, Int32 pixelHeight, Dpi dpi, ImageSource image = null)
        {
            var visual = GetDrawingVisual(pixelWidth, pixelHeight, dpi, image);

            if (visual == null)
                return;

            var printDlg = new PrintDialog();

            if ((Boolean)printDlg.ShowDialog())
            {
                var l = Math.Max(pixelWidth, pixelHeight);
                var s = Math.Min(pixelWidth, pixelHeight);

                var sizeL = Math.Max(printDlg.PrintableAreaWidth, printDlg.PrintableAreaHeight);
                var sizeS = Math.Min(printDlg.PrintableAreaWidth, printDlg.PrintableAreaHeight);

                var zoom = Math.Min(sizeL / l, sizeS / s);
                visual.Transform = new ScaleTransform(zoom, zoom);

                if (pixelWidth != pixelHeight && (pixelWidth > pixelHeight ^ printDlg.PrintableAreaWidth > printDlg.PrintableAreaHeight))
                    printDlg.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;

                printDlg.PrintVisual(visual, nameof(DrawingCanvas));
            }
        }

        public void Save(String filepath)
        {
            var serializer = new DrawGeometrySerializer
            {
                Geometries = this.GetDrawGeometries().Select(q => q.ToSerializer()).ToArray()
            };

            var xml = new XmlSerializer(typeof(DrawGeometrySerializer));

            using (var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                xml.Serialize(fs, serializer);
            }
        }

        public void Load(String filepath)
        {
            var xml = new XmlSerializer(typeof(DrawGeometrySerializer));

            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = (DrawGeometrySerializer)xml.Deserialize(fs);

                this.Clear();

                foreach (var draw in serializer.Geometries)
                {
                    this.AddVisual(draw.Deserialize(this));
                }
            }
        }

        #endregion

        #region 私有方法

        private IDrawTool CreateDrawingTool()
        {
            switch (DrawingToolType)
            {
                case DrawToolType.Pointer:
                    return new PointerDrawTool(this);
                case DrawToolType.Pen:
                    return new PenDrawTool(this);
                case DrawToolType.Eraser:
                    return new EraserDrawTool(this);
                case DrawToolType.Line:
                    return new LineDrawTool(this);
                case DrawToolType.Arrow:
                    return new ArrowDrawTool(this);
                case DrawToolType.Ranging:
                    return new RangingDrawTool(this);
                case DrawToolType.Rectangle:
                    return new RectangleDrawTool(this);
                case DrawToolType.Ellipse:
                    return new EllipseDrawTool(this);
                case DrawToolType.Angle:
                    return new AngleDrawTool(this);
                case DrawToolType.Polyline:
                    return new PolylineDrawTool(this);
                case DrawToolType.Curve:
                    return new CurveDrawTool(this);
                case DrawToolType.Polygon:
                    return new PolygonDrawTool(this);
                case DrawToolType.ClosedCurve:
                    return new ClosedCurveDrawTool(this);
                case DrawToolType.Area:
                    return new AreaDrawTool(this);
                case DrawToolType.Text:
                    return new TextDrawTool(this);
                default:
                    throw new ArgumentOutOfRangeException("不支持画图工具" + DrawingToolType);
            }
        }

        private void UpdateCursor()
        {
            switch (this.DrawingToolType)
            {
                case DrawToolType.Pointer:
                    this.OriginalCursor = Cursors.Arrow;
                    this.squareCursor = false;
                    break;
                case DrawToolType.Text:
                    this.OriginalCursor = Cursors.IBeam;
                    this.squareCursor = false;
                    break;
                default:
                    if (squareCursor && cursorLength == StrokeThickness && cursorZoom == Zoom)
                        break;
                    var w = (UInt32)Math.Max(1, StrokeThickness * Zoom);
                    var h = (UInt32)Math.Max(1, StrokeThickness * Zoom);
                    var border = (UInt32)Math.Max(1, 2 * Math.Min(1, Zoom));
                    this.OriginalCursor = DrawCursors.CreateBmpCursor(w, h, border, Brush);
                    this.squareCursor = true;
                    this.cursorLength = StrokeThickness;
                    this.cursorZoom = Zoom;
                    break;
            }
        }

        private DrawingVisual GetDrawingVisual(Int32 pixelWidth, Int32 pixelHeight, Dpi dpi, ImageSource image = null)
        {
            var drawGeometries = this.GetDrawGeometries();

            if (drawGeometries.Count() == 0)
                return null;

            var root = new DrawingVisual();

            var dc = root.RenderOpen();

            if (image != null)
                dc.DrawImage(image, new Rect(new Size(pixelWidth * dpi.Px2WpfX, pixelHeight * dpi.Px2WpfY)));

            foreach (var draw in drawGeometries)
            {
                dc.DrawDrawing(draw.Drawing);
            }

            dc.Close();

            return root;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 画笔
        /// </summary>
        public Pen Pen { get; }
        /// <summary>
        /// 选择画笔
        /// </summary>
        public Pen SelectPen { get; }
        /// <summary>
        /// 选择底色画笔
        /// </summary>
        public Pen SelectBackgroundPen { get; }

        #endregion

        #region 字段

        /// <summary>
        /// 正在进行的画图工具
        /// </summary>
        private List<IDrawTool> workingDrawTools = new List<IDrawTool>();

        private Cursor originalCursor;
        /// <summary>
        /// 原始指针
        /// </summary>
        public Cursor OriginalCursor
        {
            get => originalCursor;
            set
            {
                if (originalCursor == value)
                    return;

                originalCursor = value;

                if (!handleCursor)
                    this.Cursor = value;
            }
        }

        /// <summary>
        /// 鼠标指针是否正在被控制
        /// </summary>
        public Boolean handleCursor;

        public Boolean squareCursor;
        public UInt32 cursorLength;
        public Double cursorZoom;

        private Point? lastPoint;
        private DrawingCanvasViewer drawViewer;
        private Boolean canDragStart => this.Cursor == DrawCursors.Hand && drawViewer != null;
        private Boolean canDragMove => canDragStart && lastPoint.HasValue;

        #endregion
    }
}
