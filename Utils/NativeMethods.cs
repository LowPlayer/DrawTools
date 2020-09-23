using System;
using System.Runtime.InteropServices;

namespace DrawTools.Utils
{
    public static class NativeMethods
    {
        /// <summary>
        /// 调用win api将指定名称的打印机设置为默认打印机
        /// </summary>
        /// <param name="Name">打印机名称</param>
        /// <returns></returns>
        [DllImport("winspool.drv")]
        public static extern Boolean SetDefaultPrinter(String Name);
    }
}
