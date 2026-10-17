using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the low poly sign meshes, materials and prefabs.
/// Runs once automatically after the package is imported; rerun from Tools > Low Poly Sign > Rebuild Prefabs.
/// </summary>
[InitializeOnLoad]
public static partial class LowPolySignBuilder
{
    // Sizes in metres (1 unit = 1 Minecraft block).
    private const float W = 1.0f;           // board width
    private const float H = 0.6f;           // board height (taller than vanilla to fit 5 lines)
    private const float T = 1f / 12f;       // board thickness
    private const float Gap = 0.012f;       // groove between planks
    private const float PostSize = 0.07f;
    private const float PostHeight = 0.66f;
    private const float BoardBottom = 0.58f;

    // Oak sign palette.
    private static readonly Color PlankLight = Hex(0xC9A46C);
    private static readonly Color PlankDark = Hex(0xBE9962);
    private static readonly Color GrooveColor = Hex(0x5E4728);
    private static readonly Color PostColor = Hex(0x6B4F2E);

    private const int SubPlankLight = 0, SubPlankDark = 1, SubGroove = 2, SubPost = 3;
    private const string ScriptSuffix = "/Editor/LowPolySignBuilder.cs";
    private const int Planks = LowPolySign.MaxLines;   // one plank per text line
    private const string CheckedKey = "LowPolySign.AutoBuildChecked.v5";

    static LowPolySignBuilder()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuild;
            return;
        }
        if (SessionState.GetBool(CheckedKey, false)) return;
        SessionState.SetBool(CheckedKey, true);

        string root = Root();
        if (root == null) return;
        if (NeedsBuild(root + "/Prefabs/Sign_Standing.prefab") || NeedsBuild(root + "/Prefabs/Sign_Wall.prefab"))
            Build();
    }

    // Missing, or made by the older 3-plank version.
    private static bool NeedsBuild(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return true;
        var sign = prefab.GetComponent<LowPolySign>();
        return sign == null || !sign.IsBound;
    }

    [MenuItem("Tools/Low Poly Sign/Rebuild Prefabs")]
    public static void Build()
    {
        string root = Root();
        if (root == null)
        {
            Debug.LogError("[LowPolySign] Could not find the LowPolySign folder.");
            return;
        }

        string gen = root + "/Generated";
        EnsureFolder(gen);
        EnsureFolder(root + "/Prefabs");

        // Existing materials are kept, so colour tweaks survive a rebuild. Delete one to reset it.
        Material[] mats =
        {
            GetMaterial(gen, "Sign_PlankLight", PlankLight),
            GetMaterial(gen, "Sign_PlankDark", PlankDark),
            GetMaterial(gen, "Sign_Groove", GrooveColor),
            GetMaterial(gen, "Sign_Post", PostColor),
        };

        Mesh standing = GetMesh(gen + "/Sign_Standing_Mesh.asset", "Sign_Standing_Mesh", true);
        Mesh wall = GetMesh(gen + "/Sign_Wall_Mesh.asset", "Sign_Wall_Mesh", false);

        SavePrefab(root + "/Prefabs/Sign_Standing.prefab", "Sign_Standing", standing, mats,
            BoardCenter(true), true);
        SavePrefab(root + "/Prefabs/Sign_Wall.prefab", "Sign_Wall", wall,
            new[] { mats[0], mats[1], mats[2] }, BoardCenter(false), false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LowPolySign] Prefabs ready in {root}/Prefabs");
    }

    // ---------- geometry ----------

    // Standing: pivot on the ground under the post. Wall: pivot at the back centre of the board.
    // The readable face points toward -Z.
    private static Vector3 BoardCenter(bool standing) =>
        standing ? new Vector3(0f, BoardBottom + H * 0.5f, 0f) : new Vector3(0f, 0f, -T * 0.5f);

    private const float PlankHeight = (H - (Planks - 1) * Gap) / Planks;

    private static float PlankY(Vector3 boardCenter, int i) =>
        boardCenter.y + H * 0.5f - PlankHeight * 0.5f - i * (PlankHeight + Gap);

    private static void FillMesh(Mesh m, bool standing)
    {
        var b = new MeshBuilder(standing ? 4 : 3);
        Vector3 c = BoardCenter(standing);

        // Recessed core: shows through the gaps between planks as dark grooves.
        b.Box(c, new Vector3(W - 0.03f, H - 0.03f, T * 0.6f), SubGroove);

        // One plank per line, nudged slightly so the board looks hand built.
        float[] xNudge = { 0.004f, -0.005f, 0.002f, -0.003f, 0.005f };
        float[] zNudge = { -0.002f, 0.0015f, -0.001f, 0.001f, -0.0015f };
        for (int i = 0; i < Planks; i++)
        {
            b.Box(new Vector3(c.x + xNudge[i], PlankY(c, i), c.z + zNudge[i]),
                  new Vector3(W, PlankHeight, T),
                  i % 2 == 1 ? SubPlankDark : SubPlankLight);
        }

        if (standing)
            b.Box(new Vector3(0f, PostHeight * 0.5f, 0f), new Vector3(PostSize, PostHeight, PostSize), SubPost);

        b.Apply(m);
    }

    private class MeshBuilder
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int>[] subs;

        public MeshBuilder(int subMeshCount)
        {
            subs = new List<int>[subMeshCount];
            for (int i = 0; i < subMeshCount; i++) subs[i] = new List<int>();
        }

        public void Box(Vector3 c, Vector3 size, int sub)
        {
            Vector3 h = size * 0.5f;
            Vector3 hx = new Vector3(h.x, 0, 0), hy = new Vector3(0, h.y, 0), hz = new Vector3(0, 0, h.z);
            Face(c + hx, Vector3.right, hz, hy, sub);
            Face(c - hx, Vector3.left, hz, hy, sub);
            Face(c + hy, Vector3.up, hx, hz, sub);
            Face(c - hy, Vector3.down, hx, hz, sub);
            Face(c + hz, Vector3.forward, hx, hy, sub);
            Face(c - hz, Vector3.back, hx, hy, sub);
        }

        private void Face(Vector3 c, Vector3 n, Vector3 u, Vector3 v, int sub)
        {
            int i = verts.Count;
            Vector3 p0 = c - u - v, p1 = c - u + v, p2 = c + u + v, p3 = c + u - v;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
            for (int k = 0; k < 4; k++) normals.Add(n);

            float su = u.magnitude * 2f, sv = v.magnitude * 2f;
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, sv)); uvs.Add(new Vector2(su, sv)); uvs.Add(new Vector2(su, 0));

            // Pick the winding that makes the face point along n.
            bool forward = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) > 0f;
            if (forward) subs[sub].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            else subs[sub].AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        public void Apply(Mesh m)
        {
            m.Clear();
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.subMeshCount = subs.Length;
            for (int s = 0; s < subs.Length; s++) m.SetTriangles(subs[s], s);
            m.RecalculateBounds();
            m.RecalculateTangents();
        }
    }

    // ---------- prefab ----------

    private static void SavePrefab(string path, string name, Mesh mesh, Material[] mats, Vector3 boardCenter, bool withBack)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;

            var col = go.AddComponent<BoxCollider>();
            col.center = mesh.bounds.center;
            col.size = mesh.bounds.size;

            float frontZ = boardCenter.z - T * 0.5f - 0.006f;
            float backZ = boardCenter.z + T * 0.5f + 0.006f;

            var front = new TextMeshPro[Planks];
            var back = new TextMeshPro[withBack ? Planks : 0];
            var frontRoot = Child(go.transform, "Text_Front", new Vector3(0f, 0f, frontZ), Quaternion.identity);
            var backRoot = withBack
                ? Child(go.transform, "Text_Back", new Vector3(0f, 0f, backZ), Quaternion.Euler(0f, 180f, 0f))
                : null;

            for (int i = 0; i < Planks; i++)
            {
                float y = PlankY(boardCenter, i);
                front[i] = MakeText(frontRoot, "Line" + (i + 1), new Vector3(0f, y, 0f));
                if (withBack) back[i] = MakeText(backRoot, "Line" + (i + 1), new Vector3(0f, y, 0f));
            }

            go.AddComponent<LowPolySign>().Bind(front, back);

            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static Transform Child(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        return go.transform;
    }

    // One text row, centred on its plank so it never sits on a groove.
    private static TextMeshPro MakeText(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var t = go.AddComponent<TextMeshPro>();
        t.rectTransform.sizeDelta = new Vector2(W - 0.1f, PlankHeight);
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 0.85f;                       // glyphs about 0.06 m tall, fits the 0.11 m plank
        t.enableAutoSizing = true;                // long lines shrink instead of spilling off the board
        t.fontSizeMin = 0.3f;
        t.fontSizeMax = 0.85f;
        t.overflowMode = TextOverflowModes.Truncate;
        t.richText = true;
        t.color = Color.black;
        t.text = string.Empty;
        return t;
    }

    // ---------- assets ----------

    private static Mesh GetMesh(string path, string name, bool standing)
    {
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool create = m == null;
        if (create) m = new Mesh();
        FillMesh(m, standing);
        m.name = name;
        if (create) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    private static Material GetMaterial(string folder, string name, Color color)
    {
        string path = folder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null)
        {
            if (m.shader == null || !m.shader.isSupported) { m.shader = LitShader(); EditorUtility.SetDirty(m); }
            return m;
        }

        m = new Material(LitShader()) { name = name };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Built-in, URP and HDRP all get a lit shader that works.
    private static Shader LitShader()
    {
        string[] names = GraphicsSettings.currentRenderPipeline != null
            ? new[] { "Universal Render Pipeline/Lit", "HDRP/Lit", "Standard" }
            : new[] { "Standard" };
        foreach (string n in names)
        {
            Shader s = Shader.Find(n);
            if (s != null) return s;
        }
        return Shader.Find("Standard");
    }

    private static string Root()
    {
        foreach (string guid in AssetDatabase.FindAssets("LowPolySignBuilder t:MonoScript"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(ScriptSuffix)) return p.Substring(0, p.Length - ScriptSuffix.Length);
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }

    private static Color Hex(int rgb) =>
        new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);
}
