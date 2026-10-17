using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Minecraft-style sign that holds at most 5 lines of text.
/// Edit the lines in the Inspector, or call SetLine / SetText from code.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class LowPolySign : MonoBehaviour
{
    public const int MaxLines = 5;

    [SerializeField] private string[] lines = new string[MaxLines];
    [SerializeField, Range(1, 40)] private int maxCharsPerLine = 16;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private bool showOnBack = false;

    [Header("References (set by the prefab)")]
    [SerializeField] private TextMeshPro[] frontLines = new TextMeshPro[MaxLines];
    [SerializeField] private TextMeshPro[] backLines = new TextMeshPro[MaxLines];

    /// <summary>True when every front line has its text object hooked up.</summary>
    public bool IsBound
    {
        get
        {
            if (frontLines == null || frontLines.Length != MaxLines) return false;
            foreach (var t in frontLines) if (t == null) return false;
            return true;
        }
    }

    public Color TextColor
    {
        get => textColor;
        set { textColor = value; Refresh(); }
    }

    public bool ShowOnBack
    {
        get => showOnBack;
        set { showOnBack = value; Refresh(); }
    }

    public string GetLine(int index)
    {
        CheckIndex(index);
        Sanitize();
        return lines[index];
    }

    public void SetLine(int index, string value)
    {
        CheckIndex(index);
        Sanitize();
        lines[index] = Clean(value);
        Refresh();
    }

    /// <summary>Sets the whole sign from one string. Lines are split on '\n'; anything past line 5 is dropped.</summary>
    public void SetText(string text)
    {
        string[] parts = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
        Sanitize();
        for (int i = 0; i < MaxLines; i++)
            lines[i] = i < parts.Length ? Clean(parts[i]) : string.Empty;
        Refresh();
    }

    public void Clear() => SetText(string.Empty);

    /// <summary>Used by the prefab builder to hook up one text object per line.</summary>
    public void Bind(TextMeshPro[] front, TextMeshPro[] back)
    {
        frontLines = front;
        backLines = back;
        Refresh();
    }

    private void OnEnable() => Refresh();

    private void OnValidate()
    {
        Sanitize();
#if UNITY_EDITOR
        // Changing TMP text directly inside OnValidate triggers SendMessage warnings.
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Refresh(); };
#else
        Refresh();
#endif
    }

    public void Refresh()
    {
        Sanitize();
        for (int i = 0; i < MaxLines; i++)
        {
            // <nobr> stops wrapping; a long line shrinks to fit its plank instead.
            string text = "<nobr>" + lines[i] + "</nobr>";
            if (frontLines != null && i < frontLines.Length) Apply(frontLines[i], text, true);
            if (backLines != null && i < backLines.Length) Apply(backLines[i], text, showOnBack);
        }
    }

    private static void CheckIndex(int index)
    {
        if (index < 0 || index >= MaxLines)
            throw new ArgumentOutOfRangeException(nameof(index), $"Sign lines go from 0 to {MaxLines - 1}.");
    }

    private string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Replace("\r", string.Empty).Replace("\n", " ");
        return s.Length > maxCharsPerLine ? s.Substring(0, maxCharsPerLine) : s;
    }

    private void Sanitize()
    {
        if (lines == null) lines = new string[MaxLines];
        if (lines.Length != MaxLines) Array.Resize(ref lines, MaxLines);
        for (int i = 0; i < MaxLines; i++) lines[i] = Clean(lines[i]);
    }

    private void Apply(TextMeshPro t, string text, bool visible)
    {
        if (t == null) return;

        if (t.font == null)
        {
            try
            {
                if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                    t.font = TMP_Settings.defaultFontAsset;
            }
            catch (Exception) { /* TMP Essential Resources not imported yet */ }
        }

        var r = t.GetComponent<MeshRenderer>();
        if (r != null) r.enabled = visible;
        t.color = textColor;
        t.text = text;
    }
}
