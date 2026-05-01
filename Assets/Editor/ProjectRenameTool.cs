using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ProjectRenameTool : EditorWindow
{
    private bool useBaseName = false;
    private string baseName = "Object";

    private bool usePrefix = false;
    private string prefix = "PRE_";

    private int removeFirstChars = 0;

    private bool useSuffix = false;
    private string suffix = "_SUF";

    private int removeLastChars = 0;

    private bool useNumbering = false;
    private int baseNumber = 1;
    private int steps = 1;

    [MenuItem("Tools/Project Rename Tool")]
    public static void ShowWindow()
    {
        GetWindow<ProjectRenameTool>("Rename Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Rename Selection", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Base Name
            DrawToggleTextField("Base Name", ref useBaseName, ref baseName);

            // Prefix
            DrawToggleTextField("Add Prefix", ref usePrefix, ref prefix);

            // Remove First Chars
            removeFirstChars = DrawIntStepper("Remove First Chars", removeFirstChars);

            // Suffix
            DrawToggleTextField("Add Suffix", ref useSuffix, ref suffix);

            // Remove Last Chars
            removeLastChars = DrawIntStepper("Remove Last Chars", removeLastChars);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Numbering", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Numbered Toggle
            EditorGUILayout.BeginHorizontal();
            useNumbering = EditorGUILayout.Toggle(useNumbering, GUILayout.Width(20));
            EditorGUILayout.LabelField("Numbered", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledGroupScope(!useNumbering))
            {
                baseNumber = DrawIntStepper("Base Number", baseNumber);
                steps = DrawIntStepper("Steps", steps);
            }
        }

        EditorGUILayout.Space(10);

        // Preview (of the first object)
        DrawPreview();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Rename", GUILayout.Height(30)))
        {
            RenameSelectedObjects();
        }
    }

    private void DrawToggleTextField(string label, ref bool toggle, ref string text)
    {
        EditorGUILayout.BeginHorizontal();
        toggle = EditorGUILayout.Toggle(toggle, GUILayout.Width(20));
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        using (new EditorGUI.DisabledGroupScope(!toggle))
        {
            text = EditorGUILayout.TextField(text);
        }
        EditorGUILayout.EndHorizontal();
    }

    private int DrawIntStepper(string label, int value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(25); // Indent for items without main toggle
        EditorGUILayout.LabelField(label, GUILayout.Width(115));
        if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(25))) value--;
        value = EditorGUILayout.IntField(value, GUILayout.MinWidth(40));
        if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(25))) value++;
        EditorGUILayout.EndHorizontal();
        return value;
    }

    private void DrawPreview()
    {
        Object[] selection = Selection.objects;
        if (selection != null && selection.Length > 0)
        {
            Object first = selection[0];
            string path = AssetDatabase.GetAssetPath(first);
            if (!string.IsNullOrEmpty(path))
            {
                string oldName = Path.GetFileNameWithoutExtension(path);
                string newName = CalculateNewName(oldName, baseNumber);
                EditorGUILayout.HelpBox($"Preview: {oldName} -> {newName}", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select assets in the Project panel to see a preview.", MessageType.None);
        }
    }

    private string CalculateNewName(string currentName, int currentNumber)
    {
        string newName = currentName;

        // 1. Base Name
        if (useBaseName)
        {
            newName = baseName;
        }

        // 2. Remove First Chars
        if (removeFirstChars > 0)
        {
            if (newName.Length > removeFirstChars)
                newName = newName.Substring(removeFirstChars);
            else
                newName = "";
        }

        // 3. Remove Last Chars
        if (removeLastChars > 0)
        {
            if (newName.Length > removeLastChars)
                newName = newName.Substring(0, newName.Length - removeLastChars);
            else
                newName = "";
        }

        // 4. Prefix
        if (usePrefix)
        {
            newName = prefix + newName;
        }

        // 5. Suffix
        if (useSuffix)
        {
            newName = newName + suffix;
        }

        // 6. Numbering
        if (useNumbering)
        {
            newName = newName + currentNumber.ToString();
        }

        return newName;
    }

    private void RenameSelectedObjects()
    {
        Object[] selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("Rename Tool: No objects selected.");
            return;
        }

        // We sort by path to have a consistent order
        var sortedSelection = selection
            .Select(o => new { Obj = o, Path = AssetDatabase.GetAssetPath(o) })
            .Where(x => !string.IsNullOrEmpty(x.Path))
            .OrderBy(x => x.Path)
            .ToArray();

        if (sortedSelection.Length == 0) return;

        int currentNumber = baseNumber;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in sortedSelection)
            {
                string oldName = Path.GetFileNameWithoutExtension(item.Path);
                string newName = CalculateNewName(oldName, currentNumber);

                if (newName != oldName)
                {
                    string error = AssetDatabase.RenameAsset(item.Path, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"Error renaming {oldName} to {newName}: {error}");
                    }
                }

                if (useNumbering)
                {
                    currentNumber += steps;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
    }
}
