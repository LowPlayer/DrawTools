﻿namespace DrawTools.Serialize
{
    public sealed class DrawCurveSerializer : DrawGeometrySerializerBase
    {
        public override DrawGeometryBase Deserialize(DrawingCanvas drawingCanvas)
        {
            var draw = new CurveDrawTool(drawingCanvas);
            draw.DeserializeFrom(this);
            return draw;
        }

        #region 属性



        #endregion
    }
}
