using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UIUtil;

// Crea el botón de salir (escudo carmesí con X dorada, pixel art) en la esquina inferior derecha.
// Tools > Menú > Crear botón de salir. Se puede repetir: reconstruye el botón y vuelve a enlazarlo.
public static class BotonSalirBuilder
{
    const string BotonName = "BotonSalir";
    const string SpritePath = "Assets/Sprites/boton_salir.png";
    const float Tamano = 84f;   // 32 px del sprite a escala ~2.6x sobre 1920x1080
    const float Margen = 28f;   // separación con los bordes de la pantalla

    [MenuItem("Tools/Menú/Crear botón de salir")]
    public static void Build()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var sprite = ImportarSprite();
        if (!canvas || !sprite)
        {
            Debug.LogError(!canvas ? "[BotonSalirBuilder] No encuentro el Canvas en la escena."
                                   : "[BotonSalirBuilder] No encuentro el sprite " + SpritePath);
            return;
        }

        // Reconstruir desde cero
        var anterior = canvas.transform.Find(BotonName);
        if (anterior) Object.DestroyImmediate(anterior.gameObject);

        // Anclado a la esquina inferior derecha, con el pivote centrado para que crezca y gire sobre sí mismo
        var rt = NewUI(BotonName, canvas.transform);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Tamano, Tamano);
        rt.anchoredPosition = new Vector2(-Margen - Tamano / 2f, Margen + Tamano / 2f);
        var go = rt.gameObject;

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        // Sombra suave para despegarlo del vídeo de fondo
        var sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.55f);
        sombra.effectDistance = new Vector2(3f, -3f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SetColors(btn, new Color(1.25f, 1.15f, 1.0f), new Color(0.75f, 0.70f, 0.70f), Color.white, 0.1f);
        btn.navigation = new Navigation { mode = Navigation.Mode.None }; // no se selecciona con el teclado

        var salir = go.AddComponent<BotonSalir>();
        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(salir.Salir));

        // Debajo de los popups, para que al abrirse lo tapen
        int indice = new[] { "PopupAlumno", "PopupAviso" }
            .Select(n => canvas.transform.Find(n)).Where(p => p)
            .Select(p => p.GetSiblingIndex()).DefaultIfEmpty(canvas.transform.childCount - 1).Min();
        rt.SetSiblingIndex(indice);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Selection.activeGameObject = go;
        Debug.Log("[BotonSalirBuilder] Botón de salir creado en la esquina inferior derecha ✔");
    }

    // Importa la imagen como sprite de pixel art: sin filtrado, sin compresión y sin mipmaps
    static Sprite ImportarSprite()
    {
        if (!(AssetImporter.GetAtPath(SpritePath) is TextureImporter imp)) return null;

        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single ||
            imp.filterMode != FilterMode.Point || imp.textureCompression != TextureImporterCompression.Uncompressed ||
            imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
