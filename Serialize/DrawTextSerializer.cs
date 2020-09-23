using System;
using System.Windows;

namespace DrawTools.Serialize
{
    public sealed class DrawTextSerializer : DrawGeometrySerializerBase
    {
        public override DrawGeometryBase Deserialize(DrawingCanvas drawingCanvas)
        {
            var draw = new TextDrawTool(drawingCanvas);
            draw.DeserializeFrom(this);
            return draw;
        }

        #region 属性

        public Point StartPoint { get; set; }

        public String Text { get; set; }

        public Double Width { get; set; }

        public Double Height { get; set; }

        #endregion
    }
}
