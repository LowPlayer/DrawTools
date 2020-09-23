using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 画图几何图形基类
    /// </summary>
    public abstract class DrawGeometryBase : DrawingVisual, IDrawTool
    {
        public DrawGeometryBase(DrawingCanvas drawingCanvas)
        {
            this.drawingCanvas = drawingCanvas;
        }

        #region 鼠标键盘事件

        public virtual Boolean OnKeyDown(Key key) => false;

        public virtual Boolean OnKeyUp(Key key) => false;

        public virtual Boolean OnTouchDown(Int32 touchId, Point point) => false;

        public virtual Boolean OnTouchEnter(Point point) => false;

        public virtual Boolean OnTouchLeave(Point point) => false;

        public virtual Boolean OnTouchMove(Point point) => false;

        public virtual Boolean OnTouchUp(Point point) => false;

        #endregion

        #region 绘图事件

        public virtual void Draw()
        {
            var dc = this.RenderOpen();
            dc.DrawGeometry(pen.Brush, null, geometry);
            dc.Close();
        }

        public virtual Boolean Erase(Geometry erase)
        {
            geometry = Geometry.Combine(geometry, erase, GeometryCombineMode.Exclude, null);

            if (geometry.IsEmpty())
                return true;

            Draw();

            return false;
        }

        public virtual Boolean Select(Point point)
        {
            return geometry.FillContains(point);
        }

        public virtual Boolean Select(Geometry select)
        {
            return !Geometry.Combine(geometry, select, GeometryCombineMode.Intersect, null).IsEmpty();
        }

        public virtual Rect Selected()
        {
            if (Mode == 1)
                return selectRect;

            Mode = 1;

            var dc = this.RenderOpen();

            dc.DrawGeometry(pen.Brush, null, geometry);

            selectRect = GetRenderBounds();

            dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect);
            dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect);

            dc.Close();

            return selectRect;
        }

        public virtual void Unselected()
        {
            if (Mode == 0)
                return;

            Mode = 0;

            Draw();
        }

        protected virtual Rect GetRenderBounds()
        {
            return geometry.GetRenderBounds(this.drawingCanvas.SelectPen);
        }

        public virtual void Move(Double dx, Double dy)
        {
            if (geometry.Transform == null)
                geometry.Transform = new TranslateTransform(dx, dy);
            else
            {
                var translate = (TranslateTransform)geometry.Transform;
                translate.X += dx;
                translate.Y += dy;
            }

            if (Mode == 1)
            {
                Mode = 0;
                Selected();
            }
            else
                Draw();
        }

        public virtual void Edit() { }

        #endregion

        #region 序列化

        public virtual DrawGeometrySerializerBase ToSerializer() => null;

        public virtual void DeserializeFrom(DrawGeometrySerializerBase serializer) { }

        #endregion

        #region 属性

        public Int32 TouchId { get; protected set; }

        public Boolean CanTouchEnter { get; protected set; }

        public Boolean CanTouchLeave { get; protected set; }

        public Boolean CanTouchDown { get; protected set; }

        public Boolean CanTouchMove { get; protected set; }

        public Boolean CanTouchUp { get; protected set; }

        public Boolean CanKeyDown { get; protected set; }

        public Boolean CanKeyUp { get; protected set; }

        public Boolean IsFinish { get; protected set; }

        public DrawToolType DrawingToolType { get; protected set; }

        public Boolean CanEdit { get; protected set; }

        public Int32 Mode { get; protected set; }

        #endregion

        #region 字段

        protected DrawingCanvas drawingCanvas;
        protected Geometry geometry;
        protected PathGeometry pathGeometry => (PathGeometry)geometry;
        protected Pen pen;
        protected Rect selectRect;

        #endregion
    }
}
