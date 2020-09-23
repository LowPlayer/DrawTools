using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 橡皮擦
    /// </summary>
    public sealed class EraserDrawTool : IDrawTool
    {
        public EraserDrawTool(DrawingCanvas drawingCanvas)
        {
            this.drawingCanvas = drawingCanvas;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 公开方法

        public Boolean OnKeyDown(Key key)
        {
            throw new NotImplementedException();
        }

        public Boolean OnKeyUp(Key key)
        {
            throw new NotImplementedException();
        }

        public Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            this.CanTouchDown = false;

            this.pen = this.drawingCanvas.Pen;
            this.radius = pen.Thickness / 2;

            this.lastPoint = point;

            this.deleteDrawGeometries = new List<DrawGeometryBase>();

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(point.X - radius, point.Y - radius), IsClosed = true, IsFilled = true };
            geometry.Figures.Add(figure);

            var line = new LineSegment(new Point(point.X + radius, point.Y - radius), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(point.X + radius, point.Y + radius), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(point.X - radius, point.Y + radius), false);
            figure.Segments.Add(line);

            if (Erase(geometry))
            {
                IsFinish = true;
                return true;
            }

            this.drawingCanvas.AddWorkingDrawTool(this);

            this.CanTouchMove = true;
            this.CanTouchUp = true;

            if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                this.CanTouchLeave = true;

            this.drawingCanvas.handleCursor = true;

            return true;
        }

        public Boolean OnTouchEnter(Point point)
        {
            throw new NotImplementedException();
        }

        public Boolean OnTouchLeave(Point point)
        {
            return this.OnTouchUp(point);
        }

        public Boolean OnTouchMove(Point point)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { IsClosed = true, IsFilled = true };
            geometry.Figures.Add(figure);

            var positiveX = radius * (point.X > lastPoint.X ? 1 : -1);
            var positiveY = radius * (point.Y > lastPoint.Y ? 1 : -1);

            figure.StartPoint = new Point(lastPoint.X - positiveX, lastPoint.Y - positiveY);

            var line = new LineSegment(new Point(lastPoint.X + positiveX, lastPoint.Y - positiveY), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(point.X + positiveX, point.Y - positiveY), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(point.X + positiveX, point.Y + positiveY), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(point.X - positiveX, point.Y + positiveY), false);
            figure.Segments.Add(line);

            line = new LineSegment(new Point(lastPoint.X - positiveX, lastPoint.Y + positiveY), false);
            figure.Segments.Add(line);

            lastPoint = point;

            if (Erase(geometry))
                OnTouchUp(point);

            return true;
        }

        public Boolean OnTouchUp(Point point)
        {
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

        #region 私有方法

        private Boolean Erase(Geometry geometry)
        {
            var empty = true;

            foreach (var g in this.drawingCanvas.GetDrawGeometries())
            {
                if (g.Erase(geometry))
                    deleteDrawGeometries.Add(g);
                else
                    empty = false;
            }

            foreach (var g in deleteDrawGeometries)
            {
                this.drawingCanvas.DeleteVisual(g);
            }

            deleteDrawGeometries.Clear();

            return empty;
        }

        #endregion

        #region 属性

        public Int32 TouchId { get; private set; }

        public Boolean CanTouchEnter { get; private set; }

        public Boolean CanTouchLeave { get; private set; }

        public Boolean CanTouchDown { get; private set; }

        public Boolean CanTouchMove { get; private set; }

        public Boolean CanTouchUp { get; private set; }

        public Boolean CanKeyDown { get; private set; }

        public Boolean CanKeyUp { get; private set; }

        public Boolean IsFinish { get; private set; }

        public DrawToolType DrawingToolType => DrawToolType.Eraser;

        #endregion

        #region 字段

        private DrawingCanvas drawingCanvas;
        private Point lastPoint;
        private Pen pen;
        private Double radius;
        private List<DrawGeometryBase> deleteDrawGeometries;

        #endregion
    }
}
