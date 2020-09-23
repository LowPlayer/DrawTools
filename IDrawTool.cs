using System;
using System.Windows;
using System.Windows.Input;

namespace DrawTools
{
    /// <summary>
    /// 绘制工具接口
    /// </summary>
    public interface IDrawTool
    {
        /// <summary>
        /// 触摸Id，用于分辨多点触摸，0表示鼠标
        /// </summary>
        Int32 TouchId { get; }

        /// <summary>
        /// 是否可以处理鼠标进入事件
        /// </summary>
        Boolean CanTouchEnter { get; }

        /// <summary>
        /// 处理鼠标进入事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchEnter(Point point);

        /// <summary>
        /// 是否可以处理鼠标离开事件
        /// </summary>
        Boolean CanTouchLeave { get; }

        /// <summary>
        /// 处理鼠标离开事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchLeave(Point point);

        /// <summary>
        /// 是否可以处理鼠标按下事件
        /// </summary>
        Boolean CanTouchDown { get; }

        /// <summary>
        /// 执行鼠标按下事件
        /// </summary>
        /// <param name="touchId">触摸Id，用于分辨多点触摸，0表示鼠标</param>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchDown(Int32 touchId, Point point);

        /// <summary>
        /// 是否可以处理鼠标移动事件
        /// </summary>
        Boolean CanTouchMove { get; }

        /// <summary>
        /// 执行鼠标移动事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchMove(Point point);

        /// <summary>
        /// 是否可以处理鼠标弹起事件
        /// </summary>
        Boolean CanTouchUp { get; }

        /// <summary>
        /// 执行鼠标弹起事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchUp(Point point);

        /// <summary>
        /// 是否可以处理键盘按下事件
        /// </summary>
        Boolean CanKeyDown { get; }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        /// <param name="key"></param>
        /// <returns>事件是否已处理</returns>
        Boolean OnKeyDown(Key key);

        /// <summary>
        /// 是否可以处理键盘弹起事件
        /// </summary>
        Boolean CanKeyUp { get; }

        /// <summary>
        /// 处理键盘弹起事件
        /// </summary>
        /// <param name="key"></param>
        /// <returns>事件是否已处理</returns>
        Boolean OnKeyUp(Key key);

        /// <summary>
        /// 是否结束
        /// </summary>
        Boolean IsFinish { get; }

        /// <summary>
        /// 画图工具类型
        /// </summary>
        DrawToolType DrawingToolType { get; }
    }
}
