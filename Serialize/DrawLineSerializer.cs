using System.Windows;

namespace DrawTools.Serialize
{
    public sealed class DrawLineSerializer : DrawGeometrySerializerBase
    {
        public override DrawGeometryBase Deserialize(DrawingCanvas drawingCanvas)
        {
            var draw = new LineDrawTool(drawingCanvas);
            draw.DeserializeFrom(this);
            return draw;
        }

        #region 属性

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        #endregion
    }
}
