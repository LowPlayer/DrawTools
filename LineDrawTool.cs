using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 直线
    /// </summary>
    public sealed class LineDrawTool : DrawGeometryBase
    {
        public LineDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Line;

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

                this.geometry = new PathGeometry();

                var figure = new PathFigure { StartPoint = point };
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

            var figure = pathGeometry.Figures[0];
            var line = new LineSegment(point, true);

            if (endPoint.HasValue)
                figure.Segments[0] = line;
            else
                figure.Segments.Add(line);

            endPoint = point;

            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawLineSerializer
            {
                Color = ((SolidColorBrush)pen.Brush).Color,
                StrokeThickness = pen.Thickness,
                Geometry = geometry.ToString(),
                StartPoint = startPoint.Value,
                EndPoint = endPoint.Value
            };

            if (geometry.Transform != null)
                serializer.Matrix = geometry.Transform.Value;

            return serializer;
        }

        public override void DeserializeFrom(DrawGeometrySerializerBase serializer)
        {
            var lineSerializer = (DrawLineSerializer)serializer;

            this.pen = new Pen(new SolidColorBrush(serializer.Color), serializer.StrokeThickness);

            this.geometry = Geometry.Parse(serializer.Geometry).GetFlattenedPathGeometry();
            this.geometry.Transform = new TranslateTransform(serializer.Matrix.OffsetX, serializer.Matrix.OffsetY);

            this.startPoint = lineSerializer.StartPoint;
            this.endPoint = lineSerializer.EndPoint;

            this.IsFinish = true;

            this.Draw();
        }

        #endregion

        #region 字段

        private Point? startPoint, endPoint;

        #endregion
    }
}
