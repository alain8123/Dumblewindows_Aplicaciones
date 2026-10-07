using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Arregla el vídeo de fondo del menú y aplica un estilo sobrio y oscuro al panel.
// Se ejecuta solo una vez automáticamente al abrir menu.unity; se puede repetir desde
// Tools > Menú > Aplicar estilo.
[InitializeOnLoad]
public static class MenuMagicStyle
{
    const string ScenePath = "Assets/Scenes/menu.unity";
    const string VideoPath = "Assets/Videos/castillo_animado.mp4";
    const string PopupName = "PopupAlumno";
    const string AppliedKey = "DumbleUnity.MenuStyle.v5.Applied";

    // Paleta oscura y discreta
    static readonly Color PanelBg = new Color(0.04f, 0.04f, 0.06f, 0.62f);
    static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.10f);
    static readonly Color FieldBorder = new Color(1f, 1f, 1f, 0.35f);
    static readonly Color ButtonBg = new Color(0.12f, 0.12f, 0.15f, 0.90f);
    static readonly Color Border = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color TextMain = new Color(0.92f, 0.92f, 0.94f, 1f);
    static readonly Color TextSoft = new Color(0.75f, 0.75f, 0.80f, 0.55f);

    static MenuMagicStyle()
    {
        EditorApplication.delayCall += TryAutoApply;
    }

    static void TryAutoApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(AppliedKey)) return;
        if (SceneManager.GetActiveScene().path != ScenePath) return;
        Apply();
    }

    [MenuItem("Tools/Menú/Aplicar estilo")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        // Quitar componentes de scripts que ya no existen (MagicGlow)
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        // El popup se reconstruye entero cada vez
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(x => x && x.name == PopupName).ToList())
                Object.DestroyImmediate(t.gameObject);

        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
        T First<T>() where T : Component => all.Select(t => t.GetComponent<T>()).FirstOrDefault(c => c);

        var canvas = First<Canvas>();
        var panel = all.FirstOrDefault(t => t.name == "Panel") as RectTransform;
        var rawImage = First<RawImage>();
        var input = First<TMP_InputField>();
        var button = all.Select(t => t.GetComponent<Button>()).FirstOrDefault(b => b && b.name == "Buscar") ?? First<Button>();
        var title = all.Select(t => t.GetComponent<TextMeshProUGUI>())
            .FirstOrDefault(t => t && !t.GetComponentInParent<Button>(true) && !t.GetComponentInParent<TMP_InputField>(true));

        if (!canvas || !panel || !rawImage)
        {
            Debug.LogError("[MenuStyle] No encuentro Canvas/Panel/RawImage en la escena.");
            return;
        }

        FixVideo(all, rawImage);
        StylePanel(panel);
        if (title) StyleTitle(title);
        if (input) StyleInput(input);
        if (button) StyleButton(button);
        LayoutPanel(panel, title, input, button);
        if (button) BuildPopup(canvas, panel, button, input, title ? title.font : null);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorPrefs.SetBool(AppliedKey, true);
        Debug.Log("[MenuStyle] Estilo aplicado y ventana emergente creada ✔");
    }

    // ---------------------------------------------------------------- vídeo

    static void FixVideo(System.Collections.Generic.List<Transform> all, RawImage rawImage)
    {
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);

        // Había dos VideoPlayers pintando en la misma textura: nos quedamos con uno
        var players = all.Select(t => t.GetComponent<VideoPlayer>()).Where(v => v).ToList();
        var player = players.FirstOrDefault(p => p.name == "VideoFondo") ?? players.FirstOrDefault();
        foreach (var extra in players.Where(p => p != player))
            Object.DestroyImmediate(extra.gameObject);

        if (!player)
        {
            var go = new GameObject("VideoFondo");
            SceneManager.MoveGameObjectToScene(go, rawImage.gameObject.scene);
            player = go.AddComponent<VideoPlayer>();
        }
        player.name = "VideoFondo";
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.playOnAwake = false; // lo arranca VideoFondo cuando está preparado
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = null;
        player.aspectRatio = VideoAspectRatio.FitOutside;

        // El AspectRatioFitter dejaba la imagen a 0x0: fuera
        var fitter = rawImage.GetComponent<AspectRatioFitter>();
        if (fitter) Object.DestroyImmediate(fitter);

        var rr = rawImage.rectTransform;
        rr.SetAsFirstSibling();
        rr.anchorMin = Vector2.zero;
        rr.anchorMax = Vector2.one;
        rr.pivot = new Vector2(0.5f, 0.5f);
        rr.anchoredPosition = Vector2.zero;
        rr.sizeDelta = Vector2.zero;
        rr.localScale = Vector3.one;
        rawImage.texture = null;
        rawImage.color = Color.black;
        rawImage.raycastTarget = false;
        rawImage.enabled = true;
        rawImage.gameObject.SetActive(true);

        var vf = rawImage.GetComponent<VideoFondo>();
        if (!vf) vf = rawImage.gameObject.AddComponent<VideoFondo>();
        vf.player = player;
    }

    // ---------------------------------------------------------------- UI

    static void StylePanel(RectTransform panel)
    {
        var img = panel.GetComponent<Image>();
        img.color = PanelBg;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        SetBorder(panel.gameObject, Border);
    }

    static void StyleTitle(TMP_Text title)
    {
        title.enableVertexGradient = false;
        title.color = TextMain;
        title.fontStyle = FontStyles.Normal;
        title.characterSpacing = 2;
        title.enableAutoSizing = true;
        title.fontSizeMin = 28;
        title.fontSizeMax = 46;
        title.alignment = TextAlignmentOptions.Center;
        if (title.font) title.fontSharedMaterial = title.font.material;
        SetBorder(title.gameObject, null);
    }

    static void StyleInput(TMP_InputField input)
    {
        var r = (RectTransform)input.transform;
        r.sizeDelta = new Vector2(440f, 52f);

        var img = input.GetComponent<Image>();
        img.color = FieldBg;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = true;

        var cb = input.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.4f, 1.4f, 1.4f);
        cb.selectedColor = new Color(1.8f, 1.8f, 1.8f); // se aclara al escribir
        cb.pressedColor = new Color(1.6f, 1.6f, 1.6f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        input.colors = cb;

        input.interactable = true;
        input.readOnly = false;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 36;
        input.text = string.Empty;
        input.pointSize = 22;
        input.customCaretColor = true;
        input.caretColor = TextMain;
        input.caretWidth = 2;
        input.selectionColor = new Color(1f, 1f, 1f, 0.25f);

        // Margen interior para que el texto no pegue al borde
        if (input.textViewport)
        {
            var vp = input.textViewport;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = new Vector2(16f, 6f);
            vp.offsetMax = new Vector2(-16f, -6f);
        }
        if (input.textComponent)
        {
            var t = input.textComponent;
            t.color = TextMain;
            t.fontSize = 22;
            t.fontStyle = FontStyles.Normal;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }
        if (input.placeholder is TMP_Text ph)
        {
            ph.enabled = true;
            ph.color = TextSoft;
            ph.fontSize = 20;
            ph.fontStyle = FontStyles.Italic;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.textWrappingMode = TextWrappingModes.NoWrap;
        }
        SetBorder(input.gameObject, FieldBorder);
    }

    static void StyleButton(Button button)
    {
        var img = button.GetComponent<Image>();
        img.color = ButtonBg;
        img.pixelsPerUnitMultiplier = 1f;

        var cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.6f, 1.6f, 1.6f);
        cb.selectedColor = new Color(1.3f, 1.3f, 1.3f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label)
        {
            label.color = TextMain;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = 2;
            label.fontSize = 26;
        }
        SetBorder(button.gameObject, Border);
    }

    // Panel más alto que ancho: título arriba, campo y botón debajo
    static void LayoutPanel(RectTransform panel, TMP_Text title, TMP_InputField input, Button button)
    {
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(330f, 0f);
        panel.sizeDelta = new Vector2(420f, 600f);

        if (title)
        {
            var r = title.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -70f);
            r.sizeDelta = new Vector2(-50f, 120f);
            title.textWrappingMode = TextWrappingModes.Normal; // "Registro de / Alumnos" en dos líneas si no cabe
        }
        if (input)
        {
            var r = (RectTransform)input.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, 10f);
            r.sizeDelta = new Vector2(340f, 52f);
        }
        if (button)
        {
            var r = (RectTransform)button.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, -80f);
            r.sizeDelta = new Vector2(220f, 58f);
        }
    }

    // ---------------------------------------------------------------- ventana emergente

    static void BuildPopup(Canvas canvas, RectTransform panel, Button buscar, TMP_InputField input, TMP_FontAsset font)
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // Capa a pantalla completa: oscurece el fondo y bloquea los clics a lo de detrás
        var popupRt = NewUI(PopupName, canvas.transform);
        Stretch(popupRt);
        var dim = popupRt.gameObject.AddComponent<Image>();
        dim.color = new Color(0.10f, 0.10f, 0.12f, 0.78f); // velo gris que apaga lo de detrás
        dim.raycastTarget = true;
        popupRt.gameObject.AddComponent<CanvasGroup>();
        var popup = popupRt.gameObject.AddComponent<PopupAlumno>();

        // Ventana central
        var ventana = NewUI("Ventana", popupRt);
        ventana.anchorMin = ventana.anchorMax = ventana.pivot = new Vector2(0.5f, 0.5f);
        ventana.sizeDelta = new Vector2(460f, 460f);
        var vImg = ventana.gameObject.AddComponent<Image>();
        vImg.sprite = bgSprite;
        vImg.type = Image.Type.Sliced;
        vImg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        SetBorder(ventana.gameObject, new Color(1f, 1f, 1f, 0.18f));

        // Hueco para la imagen
        var imgRt = NewUI("Imagen", ventana);
        imgRt.anchorMin = imgRt.anchorMax = imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = new Vector2(0f, -14f);
        imgRt.sizeDelta = new Vector2(360f, 360f);
        var img = imgRt.gameObject.AddComponent<Image>();
        img.sprite = null;
        img.color = new Color(1f, 1f, 1f, 0.06f);
        img.preserveAspect = true;
        img.raycastTarget = false;
        SetBorder(imgRt.gameObject, new Color(1f, 1f, 1f, 0.15f));

        var vacioRt = NewUI("TextoVacio", imgRt);
        Stretch(vacioRt);
        var vacio = vacioRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) vacio.font = font;
        vacio.text = "Imagen";
        vacio.fontSize = 22;
        vacio.fontStyle = FontStyles.Italic;
        vacio.color = TextSoft;
        vacio.alignment = TextAlignmentOptions.Center;
        vacio.raycastTarget = false;

        // Botón X arriba a la derecha
        var xRt = NewUI("Cerrar", ventana);
        xRt.anchorMin = xRt.anchorMax = xRt.pivot = new Vector2(1f, 1f);
        xRt.anchoredPosition = new Vector2(-10f, -10f);
        xRt.sizeDelta = new Vector2(34f, 34f);
        var xImg = xRt.gameObject.AddComponent<Image>();
        xImg.sprite = uiSprite;
        xImg.type = Image.Type.Sliced;
        xImg.color = ButtonBg;
        var xBtn = xRt.gameObject.AddComponent<Button>();
        xBtn.targetGraphic = xImg;
        var cb = xBtn.colors;
        cb.highlightedColor = new Color(2.2f, 0.9f, 0.9f); // rojizo suave al pasar el ratón
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        xBtn.colors = cb;
        SetBorder(xRt.gameObject, Border);

        var xTxtRt = NewUI("X", xRt);
        Stretch(xTxtRt);
        var xTxt = xTxtRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) xTxt.font = font;
        xTxt.text = "X";
        xTxt.fontSize = 20;
        xTxt.color = TextMain;
        xTxt.alignment = TextAlignmentOptions.Center;
        xTxt.raycastTarget = false;

        // Referencias y eventos
        popup.ventana = ventana;
        popup.imagen = img;
        popup.textoVacio = vacioRt.gameObject;
        var menuGroup = panel.GetComponent<CanvasGroup>();
        if (!menuGroup) menuGroup = panel.gameObject.AddComponent<CanvasGroup>();
        popup.menuDetras = menuGroup;

        UnityEventTools.AddPersistentListener(xBtn.onClick, new UnityAction(popup.Close));

        // Buscar ya no abre el popup directamente: lo abre ConsultaAlumno al recibir la respuesta
        var consulta = buscar.GetComponent<ConsultaAlumno>();
        if (!consulta) consulta = buscar.gameObject.AddComponent<ConsultaAlumno>();
        consulta.popup = popup;     // se reasigna porque el popup se reconstruye cada vez
        consulta.inputId = input;

        for (int i = buscar.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(buscar.onClick, i);
        UnityEventTools.AddPersistentListener(buscar.onClick, new UnityAction(consulta.AlPulsarBuscar));
        EditorUtility.SetDirty(consulta);

        popupRt.SetAsLastSibling();
        popupRt.gameObject.SetActive(false); // oculto hasta pulsar Buscar
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

    // Deja como mucho un Outline fino (null = ninguno) y quita sombras/brillos
    static void SetBorder(GameObject go, Color? color)
    {
        foreach (var s in go.GetComponents<Shadow>())
            if (!(s is Outline) || color == null) Object.DestroyImmediate(s);

        if (color == null) return;
        var o = go.GetComponent<Outline>();
        if (!o) o = go.AddComponent<Outline>();
        o.effectColor = color.Value;
        o.effectDistance = new Vector2(1f, -1f);
    }
}
