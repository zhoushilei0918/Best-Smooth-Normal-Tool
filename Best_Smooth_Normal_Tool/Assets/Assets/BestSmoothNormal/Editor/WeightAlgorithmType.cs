using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 权重方式
/// </summary>
public enum WeightAlgorithmType
{
    /// <summary>
    /// 使用顶点所在两条边的夹角作为权重
    /// </summary>
    UseAngle = 0,

    /// <summary>
    /// 使用点所在两条边的叉积作为权重（面积）
    /// </summary>
    UseArea = 1
}
