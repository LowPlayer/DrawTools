using DrawTools.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 画笔
    /// </summary>
    public sealed class PenDrawTool : DrawGeometryBase
    {
        public PenDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Pen;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            this.drawingCanvas.AddWorkingDrawTool(this);
            this.drawingCanvas.AddVisual(this);
            this.drawingCanvas.handleCursor = true;

            this.CanTouchDown = false;

            this.pen = this.drawingCanvas.Pen;

            points.Add(point);

            geometry = new PathGeometry();

            var figure = new PathFigure { StartPoint = point };
            pathGeometry.Figures.Add(figure);

            this.CanTouchMove = true;
            this.CanTouchUp = true;

            if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                this.CanTouchLeave = true;

            return true;
        }

        public override Boolean OnTouchMove(Point point)
        {
            if ((point - points.Last()).Length < pen.Thickness)
                return true;

            points.Add(point);

            var figure = pathGeometry.Figures[0];
            var line = new LineSegment(point, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);

            var dc = this.RenderOpen();
            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }

        public override Boolean OnTouchUp(Point point)
        {
            if (points.Count < 2)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                geometry = geometry.GetWidenedPathGeometry(pen);
                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanTouchMove = false;
            this.CanTouchUp = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            this.drawingCanvas.handleCursor = false;

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawPenSerializer
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

        #endregion
    }
}
