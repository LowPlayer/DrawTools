using DrawTools.Serialize;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DrawTools
{
    /// <summary>
    /// 文本
    /// </summary>
    public sealed class TextDrawTool : DrawGeometryBase
    {
        public TextDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Text;

            // 准备要处理的事件
            this.CanTouchDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (!IsFinish && this.drawingCanvas.GetVisual(point) is TextDrawTool textDrawTool)
            {
                textDrawTool.TouchId = TouchId;
                textDrawTool.Edit();

                this.IsFinish = true;
                this.CanTouchDown = false;
            }
            else if (textBox == null)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);
                this.drawingCanvas.AddVisual(this);

                this.startPoint = point;
                this.pen = new Pen(this.drawingCanvas.Brush, 1);

                this.fontSize = this.drawingCanvas.FontSize;
                this.typeface = new Typeface(new FontFamily("Microsoft YaHei UI,Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                this.padding = new Thickness(pen.Thickness * 3);
                this.minWidth = this.minHeight = fontSize * 2 + pen.Thickness * 8;

                AddTextBox();
            }
            else
                OnTextBoxLostFocus(null, null);

            return true;
        }

        #endregion

        #region 绘图事件

        public override void Draw()
        {
            var dc = this.RenderOpen();
            dc.DrawRectangle(Brushes.Transparent, null, selectRect);
            dc.DrawGeometry(pen.Brush, null, geometry);
            dc.Close();
        }

        public override Boolean Select(Point point)
        {
            return selectRect.Contains(point);
        }

        public override Boolean Select(Geometry select)
        {
            var rect = select.GetRenderBounds(this.drawingCanvas.SelectPen);
            return selectRect.IntersectsWith(rect);
        }

        protected override Rect GetRenderBounds()
        {
            return selectRect;
        }

        public override void Move(Double dx, Double dy)
        {
            startPoint.X += dx;
            startPoint.Y += dy;

            selectRect.Location = startPoint;

            base.Move(dx, dy);
        }

        public override void Edit()
        {
            if (Mode == 2)
                return;

            Mode = 2;

            this.drawingCanvas.AddWorkingDrawTool(this);

            this.IsFinish = false;
            this.CanTouchDown = true;
            this.CanEdit = false;

            AddTextBox();
        }

        #endregion

        #region 序列化

        public override DrawGeometrySerializerBase ToSerializer()
        {
            var serializer = new DrawTextSerializer
            {
                Color = ((SolidColorBrush)pen.Brush).Color,
                StrokeThickness = pen.Thickness,
                Geometry = geometry.ToString(),
                StartPoint = startPoint,
                Text = text,
                Width = selectRect.Width,
                Height = selectRect.Height
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

            var textSerializer = (DrawTextSerializer)serializer;

            this.selectRect.Location = this.startPoint = textSerializer.StartPoint;
            this.text = textSerializer.Text;
            this.selectRect.Width = textSerializer.Width;
            this.selectRect.Height = textSerializer.Height;

            this.IsFinish = true;

            this.Draw();
        }

        #endregion

        #region 私有方法

        private void AddTextBox()
        {
            this.textBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = pen.Brush,
                BorderThickness = new Thickness(),
                Padding = padding,
                FontFamily = new FontFamily("Microsoft YaHei UI,Tahoma"),
                FontSize = fontSize,
                Style = null,
                FocusVisualStyle = null,
                AcceptsReturn = true,
                AcceptsTab = true,
                MinWidth = minWidth,
                MinHeight = minHeight,
                Text = text
            };

            textBox.SelectionStart = textBox.Text.Length;

            Canvas.SetLeft(textBox, startPoint.X + pen.Thickness);
            Canvas.SetTop(textBox, startPoint.Y + pen.Thickness);

            textBox.Loaded += OnTextBoxLoaded;
            textBox.SizeChanged += OnTextBoxSizeChanged;
            textBox.LostFocus += OnTextBoxLostFocus;

            this.drawingCanvas.Children.Add(textBox);
        }

        private void OnTextBoxLoaded(Object sender, RoutedEventArgs e)
        {
            textBox.Focus();

            var ch = (ContentControl)textBox.Template.FindName("PART_ContentHost", textBox);

            var tf = ((Visual)ch.Content).TransformToAncestor(textBox);
            var offset = tf.Transform(new Point());

            textPoint.X = startPoint.X + offset.X + pen.Thickness;
            textPoint.Y = startPoint.Y + offset.Y + pen.Thickness;
        }

        private void OnTextBoxSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            actualWidth = e.NewSize.Width;
            actualHeight = e.NewSize.Height;

            selectRect = new Rect(startPoint, new Size(actualWidth + pen.Thickness * 2, actualHeight + pen.Thickness * 2));

            var dc = this.RenderOpen();
            dc.DrawRectangle(Brushes.Transparent, pen, selectRect);
            dc.Close();
        }

        private void OnTextBoxLostFocus(Object sender, RoutedEventArgs e)
        {
            textBox.Loaded -= OnTextBoxLoaded;
            textBox.SizeChanged -= OnTextBoxSizeChanged;
            textBox.LostFocus -= OnTextBoxLostFocus;

            this.drawingCanvas.Focus();
            this.drawingCanvas.Children.Remove(textBox);

            if (String.IsNullOrWhiteSpace(textBox.Text))
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                text = textBox.Text;

                var formattedText = new FormattedText(text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    this.fontSize,
                    pen.Brush);

                geometry = formattedText.BuildGeometry(textPoint).GetFlattenedPathGeometry();
                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;
            this.CanTouchDown = false;
            this.CanEdit = true;

            textBox = null;
        }

        #endregion

        #region 字段

        private Double fontSize;
        private Typeface typeface;
        private Thickness padding;
        private TextBox textBox;
        private String text;
        private Point startPoint, textPoint;
        private Double minWidth, minHeight, actualWidth, actualHeight;

        #endregion
    }
}
