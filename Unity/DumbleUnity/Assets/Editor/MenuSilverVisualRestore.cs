using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MenuSilverVisualRestore
{
    const string Marker = "ProjectSettings/MenuSilverVisualRestore.v2.version";
    const string Report = "MenuSilverVisualRestoreStatus.txt";
    static readonly Color Silver = new Color(.76f, .80f, .85f, 1f);

    static MenuSilverVisualRestore() => EditorApplication.delayCall += TryApply;

    static void TryApply()
    {
        if (File.Exists(Marker)) return;
        if (SceneManager.GetActiveScene().isDirty)
        {
            File.WriteAllText(Report, "PENDING: Scene has unsaved edits; use Tools > UI > Restaurar estilo sin modificar prefabs, then save.");
            return;
        }
        Apply();
    }

[MenuItem("Tools/UI/Restaurar estilo sin modificar prefabs")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path != "Assets/Scenes/menu.unity")
        {
            File.WriteAllText(Report, "PENDING: Exit Play and open menu; Tools > UI > Restaurar estilo sin modificar prefabs.");
            return;
        }
        var roots = scene.GetRootGameObjects();
        var panels = roots.SelectMany(root => root.GetComponentsInChildren<RectTransform>(true)).Where(rect => rect.name == "Panel").ToArray();
        if (panels.Length != 1 || !panels[0].GetComponent<Image>())
        {
            File.WriteAllText(Report, "FAILED: Expected a unique menu panel with Image; nothing changed.");
            return;
        }
        var buttons = roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)).ToArray();
        var callbacks = buttons.Select(button => EditorJsonUtility.ToJson(button)).ToArray();
        var prefabRoots = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(item => PrefabUtility.IsAnyPrefabInstanceRoot(item.gameObject)).ToArray();
        var prefabPaths = prefabRoots.Select(item => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item)).ToArray();
        bool wasDirty = scene.isDirty;
        string backup = "ThemeBackups/MenuVisualRestore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        Directory.CreateDirectory(backup);
        File.Copy(scene.path, backup + "/menu.unity");
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restaurar apariencia del menu");
        foreach (var root in roots) Undo.RegisterFullObjectHierarchyUndo(root, "Restaurar apariencia del menu");
        try
        {
            var panel = panels[0];
            panel.GetComponent<Image>().color = new Color(.025f, .030f, .042f, .96f);
            var frame = Child(panel, "HogwartsSilverDecor");
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            frame.SetAsFirstSibling();
            var layout = frame.GetComponent<LayoutElement>();
            if (!layout) layout = Undo.AddComponent<LayoutElement>(frame.gameObject);
            layout.ignoreLayout = true;
            foreach (string name in new[] { "Casas" })
            {
                var unwanted = frame.Find(name);
                if (unwanted) Undo.DestroyObjectImmediate(unwanted.gameObject);
            }
            foreach (float inset in new[] { 9f, 15f })
            {
                var color = inset == 9f ? Silver : new Color(.40f, .45f, .53f, .65f);
                string suffix = inset.ToString("0");
                Line(frame, "Top" + suffix, new Vector2(0, 1), Vector2.one, new Vector2(inset, -inset - 1), new Vector2(-inset, -inset), color);
                Line(frame, "Bottom" + suffix, Vector2.zero, Vector2.right, new Vector2(inset, inset), new Vector2(-inset, inset + 1), color);
                Line(frame, "Left" + suffix, Vector2.zero, Vector2.up, new Vector2(inset, inset), new Vector2(inset + 1, -inset), color);
                Line(frame, "Right" + suffix, Vector2.right, Vector2.one, new Vector2(-inset - 1, inset), new Vector2(-inset, -inset), color);
            }
            foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
            {
                var rect = Child(frame, "Corner" + corner.x + corner.y);
                rect.anchorMin = rect.anchorMax = corner;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(corner.x == 0 ? 15 : -15, corner.y == 0 ? 15 : -15);
                rect.sizeDelta = new Vector2(7, 7);
                rect.localRotation = Quaternion.Euler(0, 0, 45);
                Paint(rect, Silver);
            }
            var title = panel.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.name == "Titulo");
            var font = title ? title.font : TMP_Settings.defaultFontAsset;
            if (!font) throw new InvalidOperationException("Missing menu font.");
            var bright = new Color(.94f, .95f, .97f, 1f);
            var muted = new Color(.58f, .63f, .70f, 1f);
            Label(frame, "Academia", "H O G W A R T S", -114f, 19f, Silver, font);
            Label(frame, "Registro", "ARCHIVO MAGICO DE ALUMNOS", -240f, 12f, muted, font);
            Label(frame, "Instruccion", "IDENTIFICADOR DEL ALUMNO", -266f, 12f, Silver, font);
            Label(frame, "Lema", "DRACO DORMIENS NUNQUAM TITILLANDUS", -513f, 10f, muted, font);
            Part(frame, "Escudo", new Vector2(0f, -65f), new Vector2(44f, 44f), Silver).localRotation = Quaternion.Euler(0f, 0f, 45f);
            Part(frame, "EscudoInterior", new Vector2(0f, -65f), new Vector2(38f, 38f), new Color(.025f, .030f, .042f, .96f)).localRotation = Quaternion.Euler(0f, 0f, 45f);
            Label(frame, "InicialEscudo", "H", -44f, 27f, bright, font);
            Part(frame, "SeparadorSuperior", new Vector2(0f, -230f), new Vector2(260f, 1f), muted);
            Part(frame, "SeparadorInferior", new Vector2(0f, -487f), new Vector2(260f, 1f), muted);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (EditorJsonUtility.ToJson(buttons[index]) != callbacks[index]) throw new InvalidOperationException("Unexpected button change.");
            }
            for (int index = 0; index < prefabRoots.Length; index++)
                if (!prefabRoots[index] || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoots[index]) != prefabPaths[index]) throw new InvalidOperationException("Prefab connection changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty && !EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save scene.");
            Undo.CollapseUndoOperations(undoGroup);
            File.WriteAllText(Marker, "2");
            File.WriteAllText(Report, "SUCCESS: Original menu crest, motto, labels and silver details restored without house names; buttons unchanged; prefab connections preserved; no prefab asset written. " + (wasDirty ? "UNSAVED: Existing edits preserved; save with Ctrl+S." : "Scene saved.") + " Backup: " + backup);
            Debug.Log("[MenuSilverVisualRestore] " + File.ReadAllText(Report));
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            File.WriteAllText(Report, "FAILED: Rolled back. " + exception.Message);
            Debug.LogException(exception);
        }
    }

    static RectTransform Part(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var rect = Child(parent, name);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Paint(rect, color);
        return rect;
    }

    static void Label(Transform parent, string name, string value, float top, float size, Color color, TMP_FontAsset font)
    {
        var rect = Child(parent, name);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, top);
        rect.sizeDelta = new Vector2(360f, 42f);
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (!text) text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    static RectTransform Child(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing) return existing;
        var created = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, "Restaurar apariencia del menu");
        created.layer = parent.gameObject.layer;
        created.transform.SetParent(parent, false);
        return (RectTransform)created.transform;
    }

    static void Paint(RectTransform rect, Color color)
    {
        var image = rect.GetComponent<Image>();
        if (!image) image = Undo.AddComponent<Image>(rect.gameObject);
        image.color = color;
        image.raycastTarget = false;
    }

    static void Line(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var rect = Child(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Paint(rect, color);
    }
}
