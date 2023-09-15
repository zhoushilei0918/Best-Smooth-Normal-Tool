# Smooth Normal Tool


#### 一. 概要：

由于最近在上前辈的 TA，遇到了顶点色和切线均被使用的情况，需要把平滑后的法线保存到模型的 UV 中的情况。于是，在每天下班之后，写了这个小工具（不算特别好，但自己用着还凑活，目前还没有很认真的测试过功能是否一定完全正确）。

---



#### 二. Unity 版本

- 使用的是 **Unity 2021.3.25f1c1** 的版本，算是一个比较新的版本。经过了测试，老的版本 2020 之前的，可能个别的函数有点不同，只需要稍作修改就可以使用，如果不会修改也可以交流联系。

---



#### 三. 使用方法说明

- 由于精力实在有限，这边简单的写一个使用说明，工程所在的目录为：

    ![image-20230915223226598](README.assets/image-20230915223226598.png)

    没错上面的 **BestSmoothNorml** 下面的都是。其中：

    - BestSmoothNormalTool.cs 为程序的主体部分；
    - Outline.mat 和 Outline.shader 则是测试用的材质；

---

- 在把上述内容导入项目之后（这边使用的是 bluid-in 渲染管线，如果要到入到 URP 的话，需要修改，但大差不差），在上面的菜单中可以看到对应的 Tool，如下图：

    ![image-20230915223549178](README.assets/image-20230915223549178.png)

    打开后面板是这样的：

    ![image-20230915223756054](README.assets/image-20230915223756054.png)

    1. 选中含有 MeshFilter 和 SkinnedMeshRenderer 物体（本身或者其子物体），程序会自动锁定；

    2. 在写入目标中选择对应的项，目前只有保存到 UV 才有八面体映射；

        ![image-20230915224232927](README.assets/image-20230915224232927.png)

        > 备注：经过测试，低版本的 shader 中只有 uv1, uv2, uv3，所以这边可能要做修改。

    3. 对于所有的平滑法线都是 [-1, -1, -1] 到 [1, 1, 1] 的，通过勾选 "是否映射到 [0, 1] " 之后，会将原来的值乘上 0.5 再加上 0.5，当初写的时候是学的 Vertex Color 和 UV 是负值不太好，但是实际测试似乎也能用。

    4. 是否使用权重的话，一种是使用平滑顶点所在的三角形的两条边的夹角作为权重，一种是用这个三角形的面积，即两条边的叉积的绝对值作为权重，经过测试效果略有不同。

        ![image-20230915224502339](README.assets/image-20230915224502339.png)

        > 备注：这边参考了一些大佬写的，如下是大佬算法的文章地址：
        >
        > - 面积加权：
        >
        >     ``` http
        >     https://zhuanlan.zhihu.com/p/546554527
        >     ```
        >
        > - 角度加权：
        >
        >     ``` http
        >     https://zhuanlan.zhihu.com/p/643206592
        >     https://www.bilibili.com/read/cv24126974/?spm_id_from=333.999.0.0
        >     ```
        >
        > 感谢大佬！

    5. 对于 "是否使用八面体算法平滑到 uv.xy" 是因为计算出来的平滑法线是三维的，当初以为 uv 只能存二维的，**结果在 Unity C# 中，顶点的 uv 是可以存四维的**：

        ``` C#
        // Mesh 类中
        public void SetUVs(int channel, Vector3[] uvs);
        public void SetUVs(int channel, List<Vector3> uvs);
        ```

        在 shader 中使用 float4 可以拿到：

        ``` shader
        struct a2v
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 color : COLOR;
            float4 tangent : TANGENT;
            float4 uv1 : TEXCOORD1; // 声明成 float4
            float4 uv2 : TEXCOORD2; // 声明成 float4
            float4 uv3 : TEXCOORD3; // 声明成 float4
            float4 uv4 : TEXCOORD4; // 声明成 float4
            float4 uv5 : TEXCOORD5; // 声明成 float4
            float4 uv6 : TEXCOORD6; // 声明成 float4
            float4 uv7 : TEXCOORD7; // 声明成 float4
        };
        ```

        而尴尬的是，还花了很多的时间，把三维的法线投影到一个二维的平面上，以便能保存到 uv 的 xy 分量，在使用的时候在 shader 中进行解压，代码主要为：

        ``` C#
        // 八面体投影算法
        private Vector2 GetOctahedronProjection(Vector3 smoothNormal)
        {
            float lenth = Mathf.Abs(smoothNormal.x) + Mathf.Abs(smoothNormal.y) + Mathf.Abs(smoothNormal.z);
            // 按八面体投影到 xy 平面
            Vector2 vector = new Vector2(smoothNormal.x / lenth, smoothNormal.y / lenth);
            // 八面体下半部分投影到四个角上，根据 |x| + |y| = 1 进行对称
            if (smoothNormal.z < 0)
            {
                vector = new Vector2(
                    (1 - Mathf.Abs(vector.y)) * (vector.x >= 0.0f ? 1f : -1f),
                    (1 - Mathf.Abs(vector.x)) * (vector.y >= 0.0f ? 1f : -1f));
            }
            return vector;
        }
        ```

        在 shader 中解压的部分：

        ``` shader
        float3 OctahedronToUnitVector(float2 oct)
        {
            float3 unitVec = float3(oct, 1 - dot(float2(1, 1), abs(oct)));
            if (unitVec.z < 0)
            {
            	unitVec.xy = (1 - abs(unitVec.yx)) * float2(unitVec.x >= 0 ? 1 : -1, unitVec.y >= 0 ? 1 : -1);
            }
            return normalize(unitVec);
        }
        ```

        > 备注：很尴尬，由于自己是数学渣，这边参考了别人的文章和现成的代码：
        >
        > ``` http
        > https://zhuanlan.zhihu.com/p/33905696
        > ```

    6. 最后，由于感觉在导出 Mesh 之后，每次都要手动挂载到原来的模型上，比较麻烦，所以加了一个自动挂载的功能。知道了上面的步骤之后，点击一下保存按钮就可以使用了。

---



#### 四.最后

- 可能会有一些版本和代码上的错误，由于写的比较着急也没有很认真的测试，我自己用着还行我就提交了。如果有什么问题可以联系我，这边的 QQ 是 1482915150，也可以提交问题到 Issue，如果我看到了一定会修改回复。
- 最后，感谢能看到这里的朋友，希望如果能帮助到你可以给我一颗星星，以后我会写更多的开源小工具。

----

