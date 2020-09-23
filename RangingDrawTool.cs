using DrawTools.Serialize;
using DrawTools.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    public sealed class RangingDrawTool : DrawGeometryBase
    {
        public RangingDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Ranging;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchLeave(Point point)
        {
            if (!endPoint.HasValue || (startPoint.Value - endPoint.Value).Length < pen.Thickness)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                var textGeometry = formattedText.BuildGeometry(textPoint);
                textGeometry.Transform = new RotateTransform(angle, center.X, center.Y);
                geometry = geometry.GetWidenedPathGeometry(pen);
                geometry = Geometry.Combine(geometry, textGeometry, GeometryCombineMode.Union, null);

                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanKeyDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            return true;
        }

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (!startPoint.HasValue)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;

                this.fontSize = this.drawingCanvas.FontSize;
                this.typeface = new Typeface(new FontFamily("Microsoft YaHei UI,Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                this.dpi = DpiHelper.GetDpiFromVisual(this.drawingCanvas);

                startPoint = point;

                geometry = new PathGeometry();

                var figure = new PathFigure();
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);

                return true;
            }
            else
                return OnTouchLeave(point);
        }

        public override Boolean OnTouchMove(Point point)
        {
            var start = startPoint.Value;

            if ((start - point).Length < pen.Thickness)
                return true;

            var dc = this.RenderOpen();

            endPoint = point;

            var x = Math.Abs(point.X - start.X) / dpi.Cm2WpfX * this.drawingCanvas.Zoom;
            var y = Math.Abs(point.Y - start.Y) / dpi.Cm2Wpfy * this.drawingCanvas.Zoom;
            var len = Math.Sqrt(x * x + y * y);
            var text = (len * 10).ToString("0.00") + "mm";

            formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush);

            center = new Point((start.X + point.X) / 2, (start.Y + point.Y) / 2);
            var width = text.Length * fontSize / 2;     // 文字宽度
            textPoint = new Point(center.X - width / 2, center.Y - fontSize * 1.2 - pen.Thickness);     // 文字左上角，1.2倍行高

            Double? k = null;       // 斜率

            if (start.X == point.X)
                angle = start.Y > point.Y ? 90 : -90;
            else
            {
                k = (point.Y - start.Y) / (point.X - start.X);
                angle = Math.Atan(k.Value) / Math.PI * 180;
            }

            dc.PushTransform(new RotateTransform(angle, center.X, center.Y));
            dc.DrawText(formattedText, textPoint);
            dc.Pop();

            var tangentK = k.HasValue ? (-1 / k) : null;
            var tangentLen = pen.Thickness + fontSize * 1.2;

            if (tangentK.HasValue)
            {
                var offsetX1 = Math.Sqrt(tangentLen * tangentLen / (1 + tangentK.Value * tangentK.Value)) * (angle > 0 ? 1 : -1);

                tangent1.X = offsetX1 + start.X;
                tangent1.Y = start.Y + offsetX1 * tangentK.Value;

                tangent2.X = offsetX1 + point.X;
                tangent2.Y = point.Y + offsetX1 * tangentK.Value;
            }
            else
            {
                tangent1.X = start.X + (angle == 90 ? tangentLen : -tangentLen);
                tangent1.Y = start.Y;

                tangent2.X = point.X + (angle == 90 ? tangentLen : -tangentLen);
                tangent2.Y = point.Y;
            }

            var figure = pathGeometry.Figures[0];
            figure.StartPoint = tangent1;
            figure.Segments.Clear();

            var line = new LineSegment(start, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);
            line = new LineSegment(point, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);
            line = new LineSegment(tangent2, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);

            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawRangingSerializer
            {
                Color = ((SolidColorBrush)pen.Brush).Color,
                StrokeThickness = pen.Thickness,
                Geometry = geometry.ToString()
            };

            if (geometry.Transform != null)
                serializer.Matrix = geometry.Transform.Value;

            return serializer;
        }

        public override void DeserializeFrom(DrawGeometrySerializerBase serializer)
        {
            this.pen = new Pen(new SolidColorBrush(serializer.Color), serializer.StrokeThickness);

            this.geometry = Geometry.Parse(serializer.Geometry).GetFlattenedPathGeometry();
            this.geometry.Transform = new TranslateTransform(serializer.Matrix.OffsetX, serializer.Matrix.OffsetY);

            this.IsFinish = true;

            this.Draw();
        }

        #endregion

        #region 字段

        private Point? startPoint, endPoint;
        private Point center, textPoint, tangent1, tangent2;
        private Double fontSize;
        private Typeface typeface;
        private FormattedText formattedText;
        private Double angle;
        private Dpi dpi;

        #endregion
    }
}
