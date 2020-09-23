using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 角度
    /// </summary>
    public sealed class AngleDrawTool : DrawGeometryBase
    {
        public AngleDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Angle;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchLeave(Point point)
        {
            if (formattedText == null)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                var textGeometry = formattedText.BuildGeometry(textPoint);
                textGeometry.Transform = new RotateTransform(textAngle, textCenterPoint.X, textCenterPoint.Y);
                geometry = geometry.GetWidenedPathGeometry(pen);
                geometry = Geometry.Combine(geometry, textGeometry, GeometryCombineMode.Union, null);

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

            if (!vertex.HasValue)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;
                this.minWidth = this.pen.Thickness * 20;
                this.arcLen = this.pen.Thickness * 15;
                this.arcTextLen = this.pen.Thickness * 25;

                this.fontSize = this.drawingCanvas.FontSize;
                this.typeface = new Typeface(new FontFamily("Microsoft YaHei UI,Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                vertex = point;

                geometry = new PathGeometry();

                var figure = new PathFigure { StartPoint = point };
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);
            }
            else if ((point - vertex.Value).Length > minWidth && !startPoint.HasValue)
            {
                startPoint = point;

                var figure = pathGeometry.Figures[0];
                figure.StartPoint = point;

                points = new Point[2];
                points[0] = vertex.Value;

                var line = new LineSegment(vertex.Value, true) { IsSmoothJoin = true };

                if (mousePoint.HasValue)
                {
                    figure.Segments[figure.Segments.Count - 1] = line;
                    mousePoint = null;
                }
                else
                    figure.Segments.Add(line);

                var dc = this.RenderOpen();
                dc.DrawGeometry(null, pen, geometry);
                dc.Close();

                if (point.X == vertex.Value.X)
                {
                    arcPoint1.X = arcTextPoint1.X = point.X;
                    arcPoint1.Y = vertex.Value.Y + (point.Y > vertex.Value.Y ? arcLen : -arcLen);
                    arcTextPoint1.Y = vertex.Value.Y + (point.Y > vertex.Value.Y ? arcTextLen : -arcTextLen);
                }
                else
                {
                    k1 = (point.Y - vertex.Value.Y) / (point.X - vertex.Value.X);

                    var offsetX = Math.Sqrt(arcLen * arcLen / (k1.Value * k1.Value + 1)) * (point.X > vertex.Value.X ? 1 : -1);

                    arcPoint1.X = vertex.Value.X + offsetX;
                    arcPoint1.Y = vertex.Value.Y + offsetX * k1.Value;

                    offsetX = Math.Sqrt(arcTextLen * arcTextLen / (k1.Value * k1.Value + 1)) * (point.X > vertex.Value.X ? 1 : -1);

                    arcTextPoint1.X = vertex.Value.X + offsetX;
                    arcTextPoint1.Y = vertex.Value.Y + offsetX * k1.Value;
                }

                figure = new PathFigure { StartPoint = arcPoint1 };
                pathGeometry.Figures.Add(figure);

                figure = new PathFigure { StartPoint = vertex.Value };
                pathGeometry.Figures.Add(figure);
            }
            else
                return OnTouchLeave(point);

            return true;
        }

        public override Boolean OnTouchMove(Point point)
        {
            if (!startPoint.HasValue)
            {
                // 画线
                var dc = this.RenderOpen();

                if ((point - vertex.Value).Length < minWidth)
                {
                    dc.Close();
                    return true;
                }

                var figure = pathGeometry.Figures[0];
                var line = new LineSegment(point, true) { IsSmoothJoin = true };

                if (mousePoint.HasValue)
                    figure.Segments[figure.Segments.Count - 1] = line;
                else
                    figure.Segments.Add(line);

                mousePoint = point;

                dc.DrawGeometry(null, pen, geometry);
                dc.Close();
            }
            else
            {
                // 画夹角
                if ((point - vertex.Value).Length < minWidth)
                    return true;

                mousePoint = points[1] = point;

                var polyLine = new PolyLineSegment(points, true);

                var figure = pathGeometry.Figures[0];
                figure.Segments[figure.Segments.Count - 1] = polyLine;

                // 画圆弧
                if (point.X == vertex.Value.X)
                {
                    arcPoint2.X = arcTextPoint2.X = point.X;
                    arcPoint2.Y = vertex.Value.Y + (point.Y > vertex.Value.Y ? arcLen : -arcLen);
                    arcTextPoint2.Y = vertex.Value.Y + (point.Y > vertex.Value.Y ? arcTextLen : -arcTextLen);
                }
                else
                {
                    k2 = (point.Y - vertex.Value.Y) / (point.X - vertex.Value.X);

                    var offsetX = Math.Sqrt(arcLen * arcLen / (k2.Value * k2.Value + 1)) * (point.X > vertex.Value.X ? 1 : -1);

                    arcPoint2.X = vertex.Value.X + offsetX;
                    arcPoint2.Y = vertex.Value.Y + offsetX * k2.Value;

                    offsetX = Math.Sqrt(arcTextLen * arcTextLen / (k2.Value * k2.Value + 1)) * (point.X > vertex.Value.X ? 1 : -1);

                    arcTextPoint2.X = vertex.Value.X + offsetX;
                    arcTextPoint2.Y = vertex.Value.Y + offsetX * k2.Value;
                }

                if (k1.HasValue)
                {
                    angle1 = Math.Atan(k1.Value) / Math.PI * 180;

                    if (arcPoint1.X < vertex.Value.X)
                        angle1 += 180;
                    else if (k1.Value < 0)
                        angle1 += 360;
                }
                else
                    angle1 = arcPoint1.Y > vertex.Value.Y ? 90 : -90;

                if (k2.HasValue)
                {
                    angle2 = Math.Atan(k2.Value) / Math.PI * 180;

                    if (arcPoint2.X < vertex.Value.X)
                        angle2 += 180;
                    else if (k2.Value < 0)
                        angle2 += 360;
                }
                else
                    angle2 = arcPoint2.Y > vertex.Value.Y ? 90 : -90;

                angle = (angle2 + 360 - angle1) % 360;
                var clockwise = angle < 180;
                var arc = new ArcSegment(arcPoint2, new Size(arcLen, arcLen), 0, false, clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true);

                figure = pathGeometry.Figures[1];

                if (figure.Segments.Count == 0)
                    figure.Segments.Add(arc);
                else
                    figure.Segments[0] = arc;

                var dc = this.RenderOpen();

                // 画文字
                angle %= 180;

                if (!clockwise)
                    angle = 180 - angle;

                var text = angle.ToString("0.00") + "°";

                formattedText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    this.fontSize,
                    pen.Brush);

                textCenterPoint.X = (arcTextPoint1.X + arcTextPoint2.X) / 2;
                textCenterPoint.Y = (arcTextPoint1.Y + arcTextPoint2.Y) / 2;

                if (textCenterPoint.X == vertex.Value.X)
                {
                    if (textCenterPoint.Y >= vertex.Value.Y)
                    {
                        textAngle = 90;
                        textCenterPoint.Y = vertex.Value.Y + arcTextLen;
                    }
                    else
                    {
                        textAngle = -90;
                        textCenterPoint.Y = vertex.Value.Y - arcTextLen;
                    }
                }
                else
                {
                    k3 = (textCenterPoint.Y - vertex.Value.Y) / (textCenterPoint.X - vertex.Value.X);

                    textAngle = Math.Atan(k3.Value) / Math.PI * 180;



                    var offsetX = Math.Sqrt(arcTextLen * arcTextLen / (k3.Value * k3.Value + 1)) * (textCenterPoint.X > vertex.Value.X ? 1 : -1);

                    textCenterPoint.X = vertex.Value.X + offsetX;
                    textCenterPoint.Y = vertex.Value.Y + offsetX * k3.Value;
                }

                figure = pathGeometry.Figures[2];
                var line = new LineSegment(textCenterPoint, true);

                if (figure.Segments.Count == 0)
                    figure.Segments.Add(line);
                else
                    figure.Segments[0] = line;

                dc.DrawGeometry(null, pen, geometry);

                dc.PushTransform(new RotateTransform(textAngle, textCenterPoint.X, textCenterPoint.Y));

                var _textAngle = textAngle;

                textPoint.X = textCenterPoint.X;
                textPoint.Y = textCenterPoint.Y - fontSize / 2;

                if (textPoint.X < vertex.Value.X)
                    _textAngle += 180;
                else if (k3.Value < 0)
                    _textAngle += 360;

                if (_textAngle >= 90 && _textAngle <= 270)
                {
                    var width = text.Length * fontSize / 2 + fontSize;
                    textPoint.X -= width;
                }
                else
                    textPoint.X += fontSize / 2;

                dc.DrawText(formattedText, textPoint);
                dc.Pop();

                dc.Close();
            }

            return true;
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawAngleSerializer
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

        private Point? vertex, startPoint, mousePoint;      // 顶点、开始描点、鼠标移动点
        private Point[] points;                             // 角度连线
        private Point arcPoint1, arcPoint2;                 // 圆弧起始点
        private Point arcTextPoint1, arcTextPoint2, textCenterPoint, textPoint;     // 文字圆弧起始点，及中点
        private Double? k1, k2, k3;                     // 两直线斜率，及文字斜率
        private Double angle, angle1, angle2, arcLen;   // 两直线夹角， 及圆弧半径，
        private Double textAngle, arcTextLen;           // 文字夹角，文字圆弧半径
        private Double minWidth;
        private Double fontSize;
        private Typeface typeface;
        private FormattedText formattedText;

        #endregion
    }
}
