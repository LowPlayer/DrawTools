using DrawTools.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 曲线
    /// </summary>
    public sealed class ClosedCurveDrawTool : DrawGeometryBase
    {
        public ClosedCurveDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.ClosedCurve;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchLeave(Point point)
        {
            var figure = pathGeometry.Figures[0];
            figure.IsClosed = true;

            if (mousePoint.HasValue)
            {
                points.Add(mousePoint.Value);
                mousePoint = null;
            }

            point = points.Last();
            var first = points[0];

            if ((point - first).Length > pen.Thickness)
            {
                var centerX = (first.X + point.X) / 2;
                var bezier = new BezierSegment(new Point(centerX, point.Y), new Point(centerX, first.Y), first, true) { IsSmoothJoin = true };

                if (mousePoint.HasValue)
                    figure.Segments[figure.Segments.Count - 1] = bezier;
                else
                {
                    figure.Segments.Add(bezier);
                    mousePoint = null;
                }
            }

            if (points.Count < 3)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                geometry = geometry.GetWidenedPathGeometry(pen);
                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            return true;
        }

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (points.Count == 0)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;

                points.Add(point);

                geometry = new PathGeometry();

                var figure = new PathFigure { StartPoint = point };
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);
            }
            else if ((point - points.Last()).Length <= pen.Thickness)
                return OnTouchLeave(point);
            else if (mousePoint.HasValue)
            {
                points.Add(mousePoint.Value);
                mousePoint = null;
            }

            return true;
        }

        public override Boolean OnTouchMove(Point point)
        {
            var last = points.Last();

            if ((point - last).Length <= pen.Thickness)
                return true;

            var figure = pathGeometry.Figures[0];
            var centerX = (last.X + point.X) / 2;
            var bezier = new BezierSegment(new Point(centerX, last.Y), new Point(centerX, point.Y), point, true) { IsSmoothJoin = true };

            if (mousePoint.HasValue)
                figure.Segments[figure.Segments.Count - 1] = bezier;
            else
                figure.Segments.Add(bezier);

            mousePoint = point;

            var dc = this.RenderOpen();
            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawClosedCurveSerializer
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

        private List<Point> points = new List<Point>();
        private Point? mousePoint;

        #endregion
    }
}
