using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ShowGameObjectName : MonoBehaviour
{
    public float Offest = 1.0f;

    public string Title = string.Empty;

    public string SaveTargetName = string.Empty;

    public bool IsMappingTo01 = false;

    public bool IsOct = false;

    private GUIStyle inner_style = null;

    private void OnDrawGizmos()
    {
        if (this.inner_style == null)
        {
            this.inner_style = new GUIStyle();
            this.inner_style.normal.textColor = Color.red;
        }
        StringBuilder builder = new StringBuilder("");
        builder.AppendLine(this.Title);
        builder.AppendLine($"平滑法线保存位置: {this.SaveTargetName}");
        builder.AppendLine($"是否映射到[0,1]: {(this.IsMappingTo01 ? "是" : "否")}");
        builder.AppendLine($"是否使用八面体算法保存 uv:{(this.IsOct ? "是" : "否")}");
        Handles.Label(this.transform.position + this.Offest * Vector3.up, builder.ToString(), this.inner_style);
    }
}
