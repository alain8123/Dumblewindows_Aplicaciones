using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Utilidades compartidas por los scripts de editor que construyen la interfaz
// (MenuMagicStyle, AvisoBuilder y BotonSalirBuilder).
public static class UIUtil
{
    // Crea un objeto de UI vacío (solo RectTransform) en la capa "UI", dentro de 'parent'.
    public static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Hace que el RectTransform ocupe todo el espacio de su padre.
    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.zero;
    }

    // Ancla, pivota y coloca el RectTransform en un punto del padre (por ejemplo (0.5, 0.5) = centro).
    public static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }

    // Deja como mucho un Outline fino de 1 px (null = ninguno) y quita sombras o brillos.
    public static void SetBorder(GameObject go, Color? color)
    {
        foreach (var s in go.GetComponents<Shadow>())
            if (!(s is Outline) || color == null) Object.DestroyImmediate(s);

        if (color == null) return;
        var o = go.GetComponent<Outline>();
        if (!o) o = go.AddComponent<Outline>();
        o.effectColor = color.Value;
        o.effectDistance = new Vector2(1f, -1f);
    }

    // Colores de un botón o campo: tinte al pasar el ratón, al pulsar y al estar seleccionado.
    // (Valores > 1 aclaran el color base del Image.)
    public static void SetColors(Selectable s, Color highlighted, Color pressed, Color selected, float fade)
    {
        var cb = s.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = highlighted;
        cb.pressedColor = pressed;
        cb.selectedColor = selected;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = fade;
        s.colors = cb;
    }

    // Crea un texto TMP que ocupa todo su padre, centrado y sin bloquear clics.
    public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
                                          TMP_FontAsset font)
    {
        var rt = NewUI(name, parent);
        Stretch(rt);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // Devuelve el componente T del objeto, añadiéndolo si no lo tiene.
    // (No se usa "??" porque Unity puede devolver un null "falso" que no es null para C#.)
    public static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    // Image con el sprite redondeado de Unity, recortable (Sliced), del color indicado.
    public static Image AddSlicedImage(GameObject go, string builtinSprite, Color color)
    {
        var img = go.AddComponent<Image>();
        img.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }
}
