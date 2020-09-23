using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 箭头
    /// </summary>
    public sealed class ArrowDrawTool : DrawGeometryBase
    {
        public ArrowDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Arrow;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchLeave(Point point)
        {
            if (!endPoint.HasValue)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                geometry = geometry.GetWidenedPathGeometry(pen);
                Draw();
            }

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanKeyDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;

            return true;
        }

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (!startPoint.HasValue)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;

                startPoint = point;

                geometry = new PathGeometry();

                var figure = new PathFigure { StartPoint = point };
                pathGeometry.Figures.Add(figure);
                figure = new PathFigure();
                pathGeometry.Figures.Add(figure);

                arrowPoints = new Point[2];

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
            var dc = this.RenderOpen();
            var start = startPoint.Value;

            if ((point - start).Length <= pen.Thickness * 6)
            {
                dc.Close();
                return true;
            }

            var figure = pathGeometry.Figures[0];

            var line = new LineSegment(point, true);

            if (endPoint.HasValue)
                figure.Segments[0] = line;
            else
                figure.Segments.Add(line);

            figure = pathGeometry.Figures[1];

            arrowPoints[0] = point;
            Double? k = null;       // 斜率
            var len = pen.Thickness * 6;

            if (start.X != point.X)
                k = (point.Y - start.Y) / (point.X - start.X);

            if (k.HasValue)
            {
                var angle = Math.Atan(k.Value) / Math.PI * 180;
                var center = new Point();
                var offsetX = Math.Sqrt(len * len / (k.Value * k.Value + 1)) * (point.X > start.X ? -1 : 1);

                center.X = point.X + offsetX;
                center.Y = point.Y + offsetX * k.Value;

                len /= 2;
                k = -1 / k;

                offsetX = Math.Sqrt(len * len / (k.Value * k.Value + 1)) * (angle > 0 ? 1 : -1);

                arrowStartPoint.X = center.X + offsetX;
                arrowStartPoint.Y = center.Y + offsetX * k.Value;

                arrowPoints[1].X = center.X - offsetX;
                arrowPoints[1].Y = center.Y - offsetX * k.Value;
            }
            else
            {
                if (start.Y > point.Y)
                {
                    // 箭头向上
                    arrowStartPoint.X = point.X - len / 2;
                    arrowPoints[1].X = point.X + len / 2;
                    arrowStartPoint.Y = arrowPoints[1].Y = point.Y + len;
                }
                else
                {
                    arrowStartPoint.X = point.X + len / 2;
                    arrowPoints[1].X = point.X - len / 2;
                    arrowStartPoint.Y = arrowPoints[1].Y = point.Y - len;
                }
            }

            figure.StartPoint = arrowStartPoint;
            var polyLine = new PolyLineSegment(arrowPoints, true);

            if (endPoint.HasValue)
                figure.Segments[0] = polyLine;
            else
                figure.Segments.Add(polyLine);

            endPoint = point;

            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawArrowSerializer
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
        private Point arrowStartPoint;
        private Point[] arrowPoints;

        #endregion
    }
}
