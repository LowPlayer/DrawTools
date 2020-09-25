using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 指针（拾取）
    /// </summary>
    public sealed class PointerDrawTool : DrawingVisual, IDrawTool
    {
        public PointerDrawTool(DrawingCanvas drawingCanvas)
        {
            this.drawingCanvas = drawingCanvas;

            // 准备要处理的事件
            this.CanTouchDown = true;

            this.selectedDrawGeometries = new List<DrawGeometryBase>();
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
            if (this.drawingCanvas.DrawingToolType != this.DrawingToolType)
            {
                Delete();
                return false;
            }

            this.TouchId = touchId;

            this.mode = 0;

            if (this.VisualParent == null)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);
                this.drawingCanvas.Insert(0, this);
            }
            else
            {
                this.drawingCanvas.MovoToHead(this);

                var visual = this.drawingCanvas.GetVisual(point);

                if (visual == this || (visual is DrawGeometryBase draw && selectedDrawGeometries.Contains(draw)))
                {
                    this.mode = 1;
                    this.lastPoint = point;
                }
            }

            this.startPoint = point;

            if (this.mode == 0)
            {
                selectRect = drawRect = null;

                this.selectedDrawGeometries.Clear();

                foreach (var draw in this.drawingCanvas.GetDrawGeometries())
                {
                    if (draw.Select(point))
                    {
                        if (selectRect.HasValue)
                            selectRect = Rect.Union(selectRect.Value, draw.Selected());
                        else
                            selectRect = draw.Selected();

                        selectedDrawGeometries.Add(draw);
                    }
                    else
                        draw.Unselected();
                }

                if (this.geometry == null)
                {
                    this.geometry = new PathGeometry();
                    var figure = new PathFigure { StartPoint = point, IsClosed = true, IsFilled = true };
                    geometry.Figures.Add(figure);
                }
                else
                    geometry.Figures[0].StartPoint = point;
            }

            this.CanTouchMove = true;
            this.CanTouchUp = true;

            if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                this.CanTouchLeave = true;

            return true;
        }

        public Boolean OnTouchEnter(Point point)
        {
            throw new NotImplementedException();
        }

        public Boolean OnTouchLeave(Point point)
        {
            return OnTouchUp(point);
        }

        public Boolean OnTouchMove(Point point)
        {
            if (mode == 0)
            {
                var figure = geometry.Figures[0];

                var topRight = new Point(point.X, startPoint.Y);
                var bottomLeft = new Point(startPoint.X, point.Y);

                if (figure.Segments.Count == 0)
                {
                    var line = new LineSegment(topRight, false);
                    figure.Segments.Add(line);

                    line = new LineSegment(point, false);
                    figure.Segments.Add(line);

                    line = new LineSegment(bottomLeft, false);
                    figure.Segments.Add(line);
                }
                else
                {
                    var line = (LineSegment)figure.Segments[0];
                    line.Point = topRight;

                    line = (LineSegment)figure.Segments[1];
                    line.Point = point;

                    line = (LineSegment)figure.Segments[2];
                    line.Point = bottomLeft;
                }

                selectedDrawGeometries.Clear();

                foreach (var draw in this.drawingCanvas.GetDrawGeometries())
                {
                    if (draw.Select(geometry))
                    {
                        if (selectRect.HasValue)
                            selectRect = Rect.Union(selectRect.Value, draw.Selected());
                        else
                            selectRect = draw.Selected();

                        selectedDrawGeometries.Add(draw);
                    }
                    else
                        draw.Unselected();
                }

                drawRect = new Rect(startPoint, point);

                var dc = this.RenderOpen();
                dc.DrawRectangle(null, this.drawingCanvas.SelectBackgroundPen, drawRect.Value);
                dc.DrawRectangle(null, this.drawingCanvas.SelectPen, drawRect.Value);
                dc.Close();
            }
            else if (mode == 1)
            {
                // 移动
                var dx = point.X - lastPoint.X;
                var dy = point.Y - lastPoint.Y;

                lastPoint = point;

                foreach (var draw in selectedDrawGeometries)
                {
                    draw.Move(dx, dy);
                }

                var rect = selectRect.Value;
                rect.X += dx;
                rect.Y += dy;
                selectRect = rect;

                var dc = this.RenderOpen();
                dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect.Value);
                dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect.Value);
                dc.Close();
            }

            return true;
        }

        public Boolean OnTouchUp(Point point)
        {
            if (mode == 0)
            {
                if (selectedDrawGeometries.Count == 0)
                {
                    Delete();
                    return true;
                }

                if (drawRect.HasValue || selectedDrawGeometries.Count > 1)
                {
                    if (drawRect.HasValue)
                        selectRect = Rect.Union(selectRect.Value, drawRect.Value);

                    var dc = this.RenderOpen();
                    dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect.Value);
                    dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect.Value);
                    dc.Close();
                }
            }
            else if (mode == 1 && startPoint == lastPoint)
            {
                var visual = this.drawingCanvas.GetVisual(point);

                if (visual is DrawGeometryBase selectDraw && selectDraw.CanEdit)
                {
                    selectDraw.Edit();

                    foreach (var draw in selectedDrawGeometries)
                    {
                        if (draw != selectDraw)
                            draw.Unselected();
                    }

                    selectedDrawGeometries.Clear();

                    Delete();
                    return true;
                }
            }

            this.CanTouchMove = false;
            this.CanTouchUp = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            return true;
        }

        #endregion

        #region 私有方法

        private void Delete()
        {
            foreach (var draw in selectedDrawGeometries)
            {
                draw.Unselected();
            }

            selectedDrawGeometries.Clear();

            this.drawingCanvas.DeleteVisual(this);
            this.drawingCanvas.DeleteWorkingDrawTool(this);

            IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchUp = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();
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

        public DrawToolType DrawingToolType => DrawToolType.Pointer;

        #endregion

        #region 字段

        private DrawingCanvas drawingCanvas;
        private Point startPoint, lastPoint;
        private List<DrawGeometryBase> selectedDrawGeometries;
        private PathGeometry geometry;
        private Rect? selectRect, drawRect;
        /// <summary>
        /// 0:拾取|1:拖动
        /// </summary>
        private Int32 mode;

        #endregion
    }
}
