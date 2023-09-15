using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 导出的目标类型
/// </summary>
public enum WriteTargetType
{
    /// <summary>
    /// 导出到顶点色
    /// </summary>
    VertexColor = 0,

    /// <summary>
    /// 导出到切线
    /// </summary>
    Tanget = 1,

    /// <summary>
    /// 导出到 2 - 7 套 uv
    /// </summary>
    UV2 = 2,
    UV3 = 3,
    UV4 = 4,
    UV5 = 5,
    UV6 = 6,
    UV7 = 7,
    UV8 = 8,
}