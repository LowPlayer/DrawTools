using System;
using System.Windows;
using System.Windows.Media;

namespace DrawTools.Utils
{
    public static class TreeHelper
    {
        /// <summary>
        /// 根据类型，查找父节点
        /// </summary>
        /// <typeparam name="T">查找对象类型</typeparam>
        /// <param name="d">查找源点</param>
        /// <param name="filter">筛选条件</param>
        /// <returns>查找对象</returns>
        public static T FindParent<T>(this DependencyObject d, Func<T, Boolean> filter = null) where T : class
        {
            if (d == null)
                return null;

            var parent = LogicalTreeHelper.GetParent(d) ?? VisualTreeHelper.GetParent(d);

            while (parent != null)
            {
                if (parent is T t)
                {
                    if (filter == null)
                        return t;
                    else if (filter.Invoke(t))
                        return t;
                }

                parent = LogicalTreeHelper.GetParent(parent) ?? VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
