using System;
using System.Reflection;

namespace AlicizaX.Editor.Extension
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EditorToolFunctionAttribute : Attribute
    {
        public string ToolMenuPath { get; private set; }
        public int MenuOrder { get; private set; }
        public MethodInfo MethodInfo { get; private set; }

        public EditorToolFunctionAttribute(string menu, int menuOrder = 0)
        {
            this.ToolMenuPath = menu;
            MenuOrder = menuOrder;
        }

        public void SetMethodInfo(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
        }
    }
}
