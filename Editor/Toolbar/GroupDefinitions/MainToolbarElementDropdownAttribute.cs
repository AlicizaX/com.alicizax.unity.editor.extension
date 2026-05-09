#if !UNITY_6000_3_OR_NEWER

using System;
using UnityEngine;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class MainToolbarElementDropdownAttribute : PropertyAttribute
    {

    }
}
#endif // !UNITY_6000_3_OR_NEWER
