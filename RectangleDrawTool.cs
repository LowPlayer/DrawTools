using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 矩形
    /// </summary>
    public sealed class RectangleDrawTool : DrawGeometryBase
    {
        public RectangleDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Rectangle;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnKeyDown(Key key)
        {
            if (key != Key.LeftShift || !bottomRight.HasValue)
                return false;

            return OnTouchMove(bottomRight.Value);
        }

        public override Boolean OnKeyUp(Key key)
        {
            if (key != Key.LeftShift || !bottomRight.HasValue)
                return false;

            return OnTouchMove(bottomRight.Value);
        }

        public override Boolean OnTouchLeave(Point point)
        {
            if (!bottomRight.HasValue)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                var figure = pathGeometry.Figures[0];

                var line = new LineSegment(new Point(bottomRight.Value.X, topLeft.Value.Y), true);
                figure.Segments.Add(line);

                line = new LineSegment(bottomRight.Value, true);
                figure.Segments.Add(line);

                line = new LineSegment(new Point(topLeft.Value.X, bottomRight.Value.Y), true);
                figure.Segments.Add(line);

                geometry = geometry.GetWidenedPathGeometry(pen);

                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;
            this.CanKeyDown = false;
            this.CanKeyUp = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            return true;
        }

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (!topLeft.HasValue)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;

                topLeft = point;

                this.geometry = new PathGeometry();

                var figure = new PathFigure { StartPoint = point, IsClosed = true };
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;
                this.CanKeyDown = true;
                this.CanKeyUp = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);
            }
            else
                return OnTouchLeave(point);

            return true;
        }

        public override Boolean OnTouchMove(Point point)
        {
            var dc = this.RenderOpen();

            var startPoint = topLeft.Value;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                var len = Math.Min(Math.Abs(point.X - startPoint.X), Math.Abs(point.Y - startPoint.Y));
                point = new Point(startPoint.X + (point.X > startPoint.X ? len : -len), startPoint.Y + (point.Y > startPoint.Y ? len : -len));
            }

            if ((startPoint - point).Length > pen.Thickness)
            {
                bottomRight = point;
                dc.DrawRectangle(null, pen, new Rect(startPoint, point));
            }

            dc.Close();

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawRectangleSerializer
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

        private Point? topLeft, bottomRight;

        #endregion
    }
}
