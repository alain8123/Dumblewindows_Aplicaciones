using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Crea el popup de avisos en la escena (mismo estilo que PopupAlumno) y lo conecta con ConsultaAlumno.
// Colócalo en una carpeta Editor (Assets/Editor) y ejecuta Tools > Menú > Crear popup de aviso.
// Se puede repetir las veces que haga falta: reconstruye el popup y vuelve a enlazarlo.
public static class AvisoBuilder
{
    const string AvisoName = "PopupAviso";

    static readonly Color ButtonBg = new Color(0.12f, 0.12f, 0.15f, 0.90f);
    static readonly Color Border = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color TextMain = new Color(0.92f, 0.92f, 0.94f, 1f);
    static readonly Color TextSoft = new Color(0.80f, 0.80f, 0.85f, 0.85f);

    [MenuItem("Tools/Menú/Crear popup de aviso")]
    public static void Build()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var consulta = Object.FindFirstObjectByType<ConsultaAlumno>();

        if (!canvas || !consulta)
        {
            Debug.LogError("[AvisoBuilder] Falta el Canvas o el componente ConsultaAlumno en la escena. " +
                           "Ejecuta antes Tools > Menú > Aplicar estilo.");
            return;
        }

        // Reconstruir desde cero
        var anterior = canvas.transform.Find(AvisoName);
        if (anterior) Object.DestroyImmediate(anterior.gameObject);

        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // Reutilizar la fuente y el grupo del menú del popup principal
        TMP_FontAsset font = null;
        CanvasGroup menuGroup = null;
        if (consulta.popup)
        {
            menuGroup = consulta.popup.menuDetras;
            if (consulta.popup.textoVacio)
            {
                var t = consulta.popup.textoVacio.GetComponent<TMP_Text>();
                if (t) font = t.font;
            }
        }

        // Capa a pantalla completa: oscurece el fondo y bloquea los clics
        var raiz = NewUI(AvisoName, canvas.transform);
        Stretch(raiz);
        var dim = raiz.gameObject.AddComponent<Image>();
        dim.color = new Color(0.10f, 0.10f, 0.12f, 0.78f);
        dim.raycastTarget = true;
        raiz.gameObject.AddComponent<CanvasGroup>();
        var aviso = raiz.gameObject.AddComponent<PopupAviso>();

        // Ventana central
        var ventana = NewUI("Ventana", raiz);
        ventana.anchorMin = ventana.anchorMax = ventana.pivot = new Vector2(0.5f, 0.5f);
        ventana.sizeDelta = new Vector2(460f, 260f);
        var vImg = ventana.gameObject.AddComponent<Image>();
        vImg.sprite = bgSprite;
        vImg.type = Image.Type.Sliced;
        vImg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        SetBorder(ventana.gameObject, new Color(1f, 1f, 1f, 0.18f));

        // Título
        var tituloRt = NewUI("Titulo", ventana);
        tituloRt.anchorMin = new Vector2(0f, 1f);
        tituloRt.anchorMax = new Vector2(1f, 1f);
        tituloRt.pivot = new Vector2(0.5f, 1f);
        tituloRt.anchoredPosition = new Vector2(0f, -24f);
        tituloRt.sizeDelta = new Vector2(-40f, 44f);
        var titulo = tituloRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) titulo.font = font;
        titulo.text = "Aviso";
        titulo.fontSize = 28;
        titulo.characterSpacing = 2;
        titulo.color = TextMain;
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.raycastTarget = false;

        // Mensaje
        var mensajeRt = NewUI("Mensaje", ventana);
        mensajeRt.anchorMin = Vector2.zero;
        mensajeRt.anchorMax = Vector2.one;
        mensajeRt.offsetMin = new Vector2(30f, 90f);
        mensajeRt.offsetMax = new Vector2(-30f, -76f);
        var mensaje = mensajeRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) mensaje.font = font;
        mensaje.text = "";
        mensaje.fontSize = 22;
        mensaje.color = TextSoft;
        mensaje.alignment = TextAlignmentOptions.Center;
        mensaje.textWrappingMode = TextWrappingModes.Normal;
        mensaje.raycastTarget = false;

        // Botón Aceptar
        var btnRt = NewUI("Aceptar", ventana);
        btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 24f);
        btnRt.sizeDelta = new Vector2(180f, 48f);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.sprite = uiSprite;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = ButtonBg;
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.6f, 1.6f, 1.6f);
        cb.selectedColor = new Color(1.3f, 1.3f, 1.3f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;
        SetBorder(btnRt.gameObject, Border);

        var lblRt = NewUI("Text (TMP)", btnRt);
        Stretch(lblRt);
        var lbl = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) lbl.font = font;
        lbl.text = "ACEPTAR";
        lbl.fontSize = 22;
        lbl.characterSpacing = 2;
        lbl.color = TextMain;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.raycastTarget = false;

        // Referencias y eventos
        aviso.ventana = ventana;
        aviso.titulo = titulo;
        aviso.mensaje = mensaje;
        aviso.menuDetras = menuGroup;
        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(aviso.Close));

        consulta.popupAviso = aviso;
        EditorUtility.SetDirty(consulta);

        raiz.SetAsLastSibling();
        raiz.gameObject.SetActive(false); // oculto hasta que haya un aviso

        var scene = consulta.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AvisoBuilder] Popup de aviso creado y enlazado con ConsultaAlumno ✔");
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.zero;
    }

    static void SetBorder(GameObject go, Color color)
    {
        var o = go.GetComponent<Outline>();
        if (!o) o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(1f, -1f);
    }
}
