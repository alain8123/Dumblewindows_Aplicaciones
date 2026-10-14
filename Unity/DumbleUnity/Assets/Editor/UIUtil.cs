using UnityEngine;
using UnityEngine.UI;

public static class UIUtil
{
    public static void SetColors(Selectable selectable, Color highlighted, Color pressed, Color selected, float fade)
    {
        var colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = selected;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = fade;
        selectable.colors = colors;
    }
}
