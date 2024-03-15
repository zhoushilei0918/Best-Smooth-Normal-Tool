using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

public class BestSmoothNormalTool : EditorWindow
{
    #region 成员字段

    /// <summary>
    /// 当前要处理的物体
    /// </summary>
    private GameObject inner_curGameObject = null;

    /// <summary>
    /// 界面是否经过了变动需要自动刷新
    /// </summary>
    private bool isRepaint = false;

    /// <summary>
    /// 当前写入的目标类型
    /// </summary>
    private WriteTargetType inner_writeTarget = WriteTargetType.VertexColor;

    /// <summary>
    /// 窗体的样式
    /// </summary>
    private GUIStyle inner_windowStyle = null;
    private GUIStyle inner_textStyle = null;
    private GUIStyle inner_gitHubStyle = null;
    private GUIStyle inner_qqStyle = null;

    /// <summary>
    /// 是否折叠配置项
    /// </summary>
    private bool inner_showFoldout = false;
    private bool inner_showAuthorFoldout = false;

    /// <summary>
    /// 是否映射到 [0, 1]
    /// </summary>
    private bool inner_isMap01 = false;

    /// <summary>
    /// 是否使用权重
    /// </summary>
    private bool inner_isUseWeight = true;

    /// <summary>
    /// 是否自动替换原来的 Mesh
    /// </summary>
    private bool inner_isAutoReplace = true;

    /// <summary>
    /// 是否对 uv 状态的法线使用 Octahedron 映射
    /// </summary>
    private bool inner_isUseOctahedron = false;

    /// <summary>
    /// 增加权重处理的方式
    /// </summary>
    private WeightAlgorithmType inner_weightAlgorithmType = WeightAlgorithmType.UseAngle;

    #endregion

    #region 打开 Best Smooth Normal Tool

    [MenuItem("Window/法线平滑小工具")]
    private static void ShowWindow()
    {
        BestSmoothNormalTool window = (BestSmoothNormalTool)EditorWindow.GetWindow(typeof(BestSmoothNormalTool), false, "法线平滑小工具");
        if (window.inner_windowStyle == null)
        {
            window.inner_windowStyle = new GUIStyle();
            window.inner_windowStyle.normal.background = (Texture2D)Resources.Load("Textures/defult_box_bg");
            window.inner_windowStyle.normal.textColor = Color.white;
            window.inner_windowStyle.border = new RectOffset(3, 3, 3, 3);
            window.inner_windowStyle.fontSize = 16;
            window.inner_windowStyle.fontStyle = FontStyle.Bold;
            window.inner_windowStyle.alignment = TextAnchor.MiddleCenter;
            window.inner_windowStyle.margin = new RectOffset(5, 5, 5, 5);
        }
        if(window.inner_textStyle == null)
        {
            window.inner_textStyle = new GUIStyle();
            window.inner_textStyle.fontStyle = FontStyle.Bold;
            window.inner_textStyle.margin = new RectOffset(5, 5, 5, 5);
            window.inner_textStyle.normal.textColor = Color.white;
        }
        if(window.inner_gitHubStyle == null)
        {
            window.inner_gitHubStyle = new GUIStyle();
            window.inner_gitHubStyle.margin = new RectOffset(5, 0, 10, 0);
            window.inner_gitHubStyle.normal.background = (Texture2D)Resources.Load("Textures/github");
        }
        if(window.inner_qqStyle == null)
        {
            window.inner_qqStyle = new GUIStyle();
            window.inner_qqStyle.margin = new RectOffset(5, 0, 10, 0);
            window.inner_qqStyle.normal.background = (Texture2D)Resources.Load("Textures/icon");
        }
        window.minSize = new Vector2(275, 518);
        window.maxSize = window.minSize;
        window.Show();
    }

    #endregion

    #region 界面刷新

    private void OnGUI()
    {
        if (this.isRepaint)
        {
            this.isRepaint = false;
            this.Repaint();
        }

        GUILayout.BeginHorizontal();
        {
            GUILayout.Box("法线平滑小工具", this.inner_windowStyle, GUILayout.Height(50), GUILayout.ExpandWidth(true));        
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        {
            GameObject selectedObj = Selection.activeGameObject;
            MeshFilter[] meshFilters = null;
            SkinnedMeshRenderer[] skinMeshRenders = null;
            if (selectedObj != null)
            {
                meshFilters = selectedObj.GetComponentsInChildren<MeshFilter>();
                skinMeshRenders = selectedObj.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (meshFilters.Length > 0 || skinMeshRenders.Length > 0)
                {
                    this.inner_curGameObject = selectedObj;
                    this.isRepaint = true;
                }
                else
                {
                    this.inner_curGameObject = null;
                    this.isRepaint = true;
                }
            }
            else
            {
                this.inner_curGameObject = null;
                this.isRepaint = true;
            }

            GUILayout.Space(5);
            EditorGUILayout.ObjectField("选择一个 Mesh 物体", this.inner_curGameObject, typeof(GameObject), true);
            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(this.inner_curGameObject == null);
            this.inner_writeTarget = (WriteTargetType)EditorGUILayout.EnumPopup("写入目标", this.inner_writeTarget);

            GUILayout.Space(5);
            this.inner_showFoldout = EditorGUILayout.Foldout(this.inner_showFoldout, "平滑参数设置");
            if (this.inner_showFoldout)
            {
                GUILayout.Space(5);
                this.inner_isMap01 = GUILayout.Toggle(this.inner_isMap01, "  是否值映射到 [0, 1]");
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                this.inner_isUseWeight = GUILayout.Toggle(this.inner_isUseWeight, "  是否使用权重");
                if (this.inner_isUseWeight)
                {
                    GUILayout.Label("     ");
                    this.inner_weightAlgorithmType = (WeightAlgorithmType)EditorGUILayout.EnumPopup(this.inner_weightAlgorithmType);
                }
                EditorGUILayout.EndHorizontal();

                if (this.inner_writeTarget != WriteTargetType.Tanget && this.inner_writeTarget != WriteTargetType.VertexColor)
                {
                    GUILayout.Space(5);
                    this.inner_isUseOctahedron = GUILayout.Toggle(this.inner_isUseOctahedron, "  是否使用八面体算法平滑法线到 uv.xy");
                }
            }

            GUILayout.Space(15);
            this.inner_isAutoReplace = GUILayout.Toggle(this.inner_isAutoReplace, "  是否自动替换原来的 Mesh");
            GUILayout.Space(8);
            if (GUILayout.Button("保存平滑数据为新 Mesh，并导出", GUILayout.Height(30)) && this.inner_curGameObject != null)
            {
                ExportMeshes(meshFilters, skinMeshRenders);
            }

            EditorGUI.EndDisabledGroup();
            //GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            // 作者信息
            this.inner_showAuthorFoldout = EditorGUILayout.Foldout(this.inner_showAuthorFoldout, "作者信息");
            if (this.inner_showAuthorFoldout)
            {
                GUILayout.BeginVertical(this.inner_windowStyle, GUILayout.Height(120), GUILayout.ExpandWidth(true));
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("", this.inner_gitHubStyle, GUILayout.Height(32), GUILayout.Width(32));
                        GUILayout.BeginVertical();
                        GUILayout.Label("项目所在 GitHub", this.inner_textStyle);
                        GUILayout.TextArea("https://github.com/zhoushilei0918/BestSmoothNormalTool", GUILayout.ExpandWidth(true));
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("", this.inner_qqStyle, GUILayout.Height(32), GUILayout.Width(32));
                        GUILayout.BeginVertical();
                        GUILayout.Label("如果有 bug 请提交至 issue 或联系 QQ", this.inner_textStyle);
                        GUILayout.TextArea("1482915150", GUILayout.Width(100), GUILayout.Height(20));
                        GUILayout.EndVertical();

                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);
                    GUILayout.Label("感谢支持，欢迎多多交流!~", this.inner_textStyle);
                }
                GUILayout.EndVertical();
            }
        }
    }

    #endregion

    #region 保存或导出 Mesh

    private void ExportMeshes(MeshFilter[] meshFilters, SkinnedMeshRenderer[] skinMeshRenders)
    {
        bool isStop = false;
        // MeshFilter
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            var ret = ExportMeshesCore(mesh);
            if (!ret.Item1)
            {
                isStop = true;
                break;
            }
            if (this.inner_isAutoReplace)
            {
                meshFilter.sharedMesh = ret.Item2;
            }
        }
        // SkinnedMeshRenderer
        if (!isStop)
        {
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinMeshRenders)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                var ret = ExportMeshesCore(mesh);
                if (!ret.Item1)
                {
                    break;
                }
                if (this.inner_isAutoReplace)
                {
                    skinnedMeshRenderer.sharedMesh = ret.Item2;
                }
            }
        }
    }

    private (bool, Mesh) ExportMeshesCore(Mesh mesh)
    {
        string dirPath = Path.Combine(Application.dataPath, "BestSmoothNormalTool");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        string typeName = Enum.GetName(typeof(WriteTargetType), this.inner_writeTarget);
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", dirPath, $"{mesh.name}_{typeName}", "asset");
        string relativePath = FileUtil.GetProjectRelativePath(path);
        if (string.IsNullOrEmpty(path))
        {
            return (false, null);
        }
        Mesh copyMesh = new Mesh();
        CopyMesh(copyMesh, mesh);
        Vector3[] averageNormals = AverageNormal(copyMesh);
        Mesh meshExport = ImportSmoothNormalData2Mesh(copyMesh, averageNormals);
        AssetDatabase.CreateAsset(meshExport, relativePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 加载这个 Assets 
        Mesh curMesh = AssetDatabase.LoadAssetAtPath<Mesh>(relativePath);
        return (true, curMesh);
    }

    #endregion

    #region 平滑法线

    private Vector3[] AverageNormal(Mesh mesh)
    {
        Vector3[] smoothNormals = mesh.normals;
        // 如果使用权重
        if (this.inner_isUseWeight)
        {
            // 创建一个字典，存顶点-顶点所有法线的键值对，顶点由唯一的Vector3来确定
            Dictionary<Vector3, List<WeightedNormal>> normalDict = new Dictionary<Vector3, List<WeightedNormal>>();
            // 遍历三角形
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            for (int i = 0; i < triangles.Length - 3; i += 3)
            {
                // 第 i 个三角形的三个顶点的索引分别是 i*3, i*3+1, i*3=2
                int[] triangle = new int[] { triangles[i], triangles[i + 1], triangles[i + 2] };
                for (int j = 0; j < 3; j++)
                {
                    int vertexIndex = triangle[j];
                    Vector3 vertex = vertices[vertexIndex];
                    if (!normalDict.ContainsKey(vertex))
                    {
                        normalDict.Add(vertex, new List<WeightedNormal>());
                    }

                    WeightedNormal weightedNormal;

                    //获取当前顶点出发的两条边
                    Vector3 lineA = Vector3.zero;
                    Vector3 lineB = Vector3.zero;
                    if (j == 0)
                    {
                        lineA = mesh.vertices[triangle[1]] - vertex;
                        lineB = mesh.vertices[triangle[2]] - vertex;
                    }
                    else if (j == 1)
                    {
                        lineA = mesh.vertices[triangle[2]] - vertex;
                        lineB = mesh.vertices[triangle[0]] - vertex;
                    }
                    else
                    {
                        lineA = mesh.vertices[triangle[0]] - vertex;
                        lineB = mesh.vertices[triangle[1]] - vertex;
                    }

                    float curWeight = 0.0f;
                    if (this.inner_weightAlgorithmType == WeightAlgorithmType.UseAngle)
                    {
                        /* https://www.bilibili.com/read/cv27148724 处理精度的优化 */
                        lineA *= 10000.0f;
                        lineB *= 10000.0f;
                        // 把角度作为权重，记录起来
                        float angle = Mathf.Acos(Mathf.Max(Mathf.Min(Vector3.Dot(lineA, lineB) / (lineA.magnitude * lineB.magnitude), 1), -1));
                        curWeight = angle;

                    }
                    else if (this.inner_weightAlgorithmType == WeightAlgorithmType.UseArea)
                    {
                        // 将面积作为权重
                        // C^2 = cross(A, B)^2 = A^2 * B^2 - dot(A, B)^2
                        float AdotA = Vector3.Dot(lineA, lineA);
                        float BdotB = Vector3.Dot(lineB, lineB);
                        float AdotB = Vector3.Dot(lineA, lineB);
                        curWeight = math.sqrt(AdotA * BdotB - AdotB * AdotB);
                        //curWeight = Vector3.Magnitude(Vector3.Cross(lineA, lineB));
                    }

                    weightedNormal.normal = Vector3.Cross(lineA, lineB).normalized;
                    weightedNormal.weight = curWeight;
                    normalDict[vertex].Add(weightedNormal);
                }
            }

            // 进行平均
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                if (!normalDict.ContainsKey(vertex))
                {
                    continue;
                }

                List<WeightedNormal> normalList = normalDict[vertex];
                Vector3 smoothNormal = Vector3.zero;

                // 计算权重（角度）值的总和
                float weightSum = 0;
                for (int j = 0; j < normalList.Count; j++)
                {
                    WeightedNormal weightedNormal = normalList[j];
                    weightSum += weightedNormal.weight;
                }

                // 合计加权之后的法线
                for (int j = 0; j < normalList.Count; j++)
                {
                    WeightedNormal weightedNormal = normalList[j];
                    smoothNormal += weightedNormal.normal * weightedNormal.weight / weightSum;
                }

                // 归一化
                smoothNormals[i] = smoothNormal.normalized;
                Vector4 T = tangents[i];
                Vector3 N = normals[i];
                Vector3 B = (Vector3.Cross(N, T) * T.w).normalized;
                Matrix4x4 TBN = new Matrix4x4(T, B, N, Vector3.zero);
                TBN = TBN.transpose;
                smoothNormals[i] = TBN.MultiplyVector(smoothNormals[i]).normalized;             
            }

            //mesh.SetUVs(7, smoothNormals);
            return smoothNormals;
        }
        else
        {
            Dictionary<Vector3, Vector3> averageNormalHash = new Dictionary<Vector3, Vector3>();
            for (var j = 0; j < mesh.vertexCount; j++)
            {
                if (!averageNormalHash.ContainsKey(mesh.vertices[j]))
                {
                    averageNormalHash.Add(mesh.vertices[j], mesh.normals[j]);
                }
                else
                {
                    averageNormalHash[mesh.vertices[j]] = (averageNormalHash[mesh.vertices[j]] + mesh.normals[j]).normalized;
                }
            }
            for (var j = 0; j < mesh.vertexCount; j++)
            {
                smoothNormals[j] = averageNormalHash[mesh.vertices[j]];
            }
            return smoothNormals;
        }
    }

    #endregion

    #region 将平滑之后的法线写入目标中

    private Mesh ImportSmoothNormalData2Mesh(Mesh mesh, Vector3[] averageNormals)
    {
        // 复制 Mesh
        int count = mesh.vertexCount;
        // 写入顶点色中
        if (this.inner_writeTarget == WriteTargetType.VertexColor)
        {
            Color[] colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                Vector4 cur = averageNormals[i];
                colors[i] = new Color(
                    this.inner_isMap01 ? cur.x * 0.5f + 0.5f : cur.x,
                    this.inner_isMap01 ? cur.y * 0.5f + 0.5f : cur.y,
                    this.inner_isMap01 ? cur.z * 0.5f + 0.5f : cur.z,
                    mesh.colors == null || mesh.colors.Length == 0 ? 0.0f : mesh.colors[i].a);
            }
            mesh.colors = colors;
        }
        else if (this.inner_writeTarget == WriteTargetType.Tanget)// 保存到切线中
        {
            Vector4[] tangents = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                Vector4 cur = averageNormals[i];
                tangents[i] = new Vector4(
                    this.inner_isMap01 ? cur.x * 0.5f + 0.5f : cur.x,
                    this.inner_isMap01 ? cur.y * 0.5f + 0.5f : cur.y,
                    this.inner_isMap01 ? cur.z * 0.5f + 0.5f : cur.z,
                    mesh.tangents == null || mesh.tangents.Length == 0 ? 0.0f : mesh.tangents[i].w);
            }
            mesh.tangents = tangents;
        }
        else
        {
            Vector3[] uvs = new Vector3[count];
            Vector2[] uvs_oct = new Vector2[count];
            for(int i =0; i< averageNormals.Length; i++)
            {
                if (this.inner_isUseOctahedron)
                {
                    Vector2 octNormal = GetOctahedronProjection(averageNormals[i]);
                    if (this.inner_isMap01)
                    {
                        octNormal = new Vector2(octNormal.x * 0.5f + 0.5f, octNormal.y * 0.5f + 0.5f);
                    }
                    uvs_oct[i] = new Vector2(octNormal.x, octNormal.y);

                }
                else
                {
                    uvs[i] = new Vector3(
                        this.inner_isMap01 ? averageNormals[i].x * 0.5f + 0.5f : averageNormals[i].x,
                        this.inner_isMap01 ? averageNormals[i].y * 0.5f + 0.5f : averageNormals[i].y,
                        this.inner_isMap01 ? averageNormals[i].z * 0.5f + 0.5f : averageNormals[i].z);
                }
            }
            if (this.inner_isUseOctahedron)
            {
                // 保存到对应的 uv.xy
                mesh.SetUVs((int)this.inner_writeTarget - 1, uvs_oct);
            }
            else
            {
                /* Mesh 的 uv 是可以存 4 个分量，即 x、y、z 和 w */
                // 保存到对应的 uv.xyz
                mesh.SetUVs((int)this.inner_writeTarget - 1, uvs);
            }
        }
        return mesh;
    }

    #endregion

    #region 八面体投影算法

    private Vector2 GetOctahedronProjection(Vector3 smoothNormal)
    {
        float lenth = Mathf.Abs(smoothNormal.x) + Mathf.Abs(smoothNormal.y) + Mathf.Abs(smoothNormal.z);
        // 按八面体投影到 xz 平面
        Vector2 vector = new Vector2(smoothNormal.x / lenth, smoothNormal.y / lenth);
        // 八面体下半部分投影到四个角上，根据 |x| + |z| = 1 进行对称
        if (smoothNormal.z < 0)
        {
            vector = new Vector2(
                (1 - Mathf.Abs(vector.y)) * (vector.x >= 0.0f ? 1f : -1f),
                (1 - Mathf.Abs(vector.x)) * (vector.y >= 0.0f ? 1f : -1f));
        }
        return vector;
    }

    #endregion

    #region 复制 Mesh

    private void CopyMesh(Mesh destMesh, Mesh srcMesh)
    {
        destMesh.Clear();
        destMesh.vertices = srcMesh.vertices;
        List<Vector4> uvs = new List<Vector4>();
        srcMesh.GetUVs(0, uvs);
        destMesh.SetUVs(0, uvs);
        srcMesh.GetUVs(1, uvs);
        destMesh.SetUVs(1, uvs);
        srcMesh.GetUVs(2, uvs);
        destMesh.SetUVs(2, uvs);
        srcMesh.GetUVs(3, uvs);
        destMesh.SetUVs(3, uvs);
        destMesh.normals = srcMesh.normals;
        destMesh.tangents = srcMesh.tangents;
        destMesh.boneWeights = srcMesh.boneWeights;
        destMesh.colors = srcMesh.colors;
        destMesh.colors32 = srcMesh.colors32;
        destMesh.bindposes = srcMesh.bindposes;
        destMesh.subMeshCount = srcMesh.subMeshCount;
        for (int i = 0; i < srcMesh.subMeshCount; i++)
        {
            destMesh.SetIndices(srcMesh.GetIndices(i), srcMesh.GetTopology(i), i);
        }
        destMesh.name = srcMesh.name;
        // 复制 BlendShape
        int vertexCount = srcMesh.vertices.Length;
        for (int i = 0; i < srcMesh.blendShapeCount; i++)
        {
            string shapeName = srcMesh.GetBlendShapeName(i);
            int frameCount = srcMesh.GetBlendShapeFrameCount(i);

            List<Vector3[]> vertexsList = new List<Vector3[]>();
            List<Vector3[]> normalsList = new List<Vector3[]>();
            List<Vector3[]> tangentsList = new List<Vector3[]>();
            float[] weight = new float[frameCount];
            for (int j = 0; j < frameCount; j++)
            {
                Vector3[] vertexs = new Vector3[vertexCount];
                Vector3[] normals = new Vector3[vertexCount];
                Vector3[] tangents = new Vector3[vertexCount];
                srcMesh.GetBlendShapeFrameVertices(i, j, vertexs, normals, tangents);
                vertexsList.Add(vertexs);
                normalsList.Add(normals);
                tangentsList.Add(tangents);
                weight[j] = srcMesh.GetBlendShapeFrameWeight(i, j);
            }

            for (int j = 0; j < frameCount; j++)
            {
                destMesh.AddBlendShapeFrame(shapeName, weight[j], vertexsList[j], normalsList[j], tangentsList[j]);
            }
        }
    }

    #endregion
}
