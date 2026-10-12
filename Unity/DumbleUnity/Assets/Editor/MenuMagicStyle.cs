using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using static UIUtil;

// Arregla el vídeo de fondo del menú, aplica un estilo sobrio y oscuro al panel
// y construye la ventana emergente del alumno (PopupAlumno).
// Se ejecuta sola una vez al abrir menu.unity; se puede repetir desde Tools > Menú > Aplicar estilo.
[InitializeOnLoad]
public static class MenuMagicStyle
{
    const string ScenePath = "Assets/Scenes/menu.unity";
    const string VideoPath = "Assets/Videos/castillo_animado.mp4";
    const string PopupName = "PopupAlumno";
    const string AppliedKey = "DumbleUnity.MenuStyle.v5.Applied"; // marca "ya aplicado" guardada en EditorPrefs

    // Paleta oscura y discreta
    static readonly Color PanelBg = new Color(0.04f, 0.04f, 0.06f, 0.62f);
    static readonly Color FieldBg = new Color(1f, 1f, 1f, 0.10f);
    static readonly Color FieldBorder = new Color(1f, 1f, 1f, 0.35f);
    static readonly Color ButtonBg = new Color(0.12f, 0.12f, 0.15f, 0.90f);
    static readonly Color Border = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color TextMain = new Color(0.92f, 0.92f, 0.94f, 1f);
    static readonly Color TextSoft = new Color(0.75f, 0.75f, 0.80f, 0.55f);

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    // Al cargar el editor se intenta aplicar el estilo automáticamente (solo la primera vez)
    static MenuMagicStyle() => EditorApplication.delayCall += TryAutoApply;

    static void TryAutoApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(AppliedKey)) return;
        if (SceneManager.GetActiveScene().path == ScenePath) Apply();
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

        // Todos los objetos de la escena, incluidos los desactivados
        List<Transform> All() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();

        // Limpieza: componentes de scripts que ya no existen (MagicGlow) y el popup anterior, que se reconstruye entero
        foreach (var t in All()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach (var t in All().Where(x => x && x.name == PopupName).ToList())
            if (t) Object.DestroyImmediate(t.gameObject);

        // Buscar las piezas del menú
        var all = All();
        T First<T>() where T : Component => all.Select(t => t.GetComponent<T>()).FirstOrDefault(c => c);
        var canvas = First<Canvas>();
        var panel = all.FirstOrDefault(t => t.name == "Panel") as RectTransform;
        var rawImage = First<RawImage>();
        var input = First<TMP_InputField>();
        var button = all.Select(t => t.GetComponent<Button>()).FirstOrDefault(b => b && b.name == "Buscar") ?? First<Button>();
        // El título es el primer texto que no pertenece ni a un botón ni al campo de texto
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

    // Deja un único VideoPlayer pintando el vídeo del castillo en el RawImage de fondo (a pantalla completa)
    static void FixVideo(List<Transform> all, RawImage rawImage)
    {
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
        player.clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        player.playOnAwake = false; // lo arranca VideoFondo cuando está preparado
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = null;
        player.aspectRatio = VideoAspectRatio.FitOutside;

        // El AspectRatioFitter dejaba la imagen a 0x0: fuera
        var fitter = rawImage.GetComponent<AspectRatioFitter>();
        if (fitter) Object.DestroyImmediate(fitter);

        rawImage.rectTransform.SetAsFirstSibling(); // detrás de todo
        Stretch(rawImage.rectTransform);
        rawImage.rectTransform.localScale = Vector3.one;
        rawImage.texture = null;
        rawImage.color = Color.black;
        rawImage.raycastTarget = false;
        rawImage.enabled = true;
        rawImage.gameObject.SetActive(true);

        GetOrAdd<VideoFondo>(rawImage.gameObject).player = player;
    }

    // ---------------------------------------------------------------- estilo del menú

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
        title.enableAutoSizing = true; // entre 28 y 46 según quepa
        title.fontSizeMin = 28;
        title.fontSizeMax = 46;
        title.alignment = TextAlignmentOptions.Center;
        if (title.font) title.fontSharedMaterial = title.font.material; // material limpio, sin brillos
        SetBorder(title.gameObject, null);
    }

    static void StyleInput(TMP_InputField input)
    {
        var img = input.GetComponent<Image>();
        img.color = FieldBg;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = true;
        // se aclara al pasar el ratón y aún más al escribir
        SetColors(input, new Color(1.4f, 1.4f, 1.4f), new Color(1.6f, 1.6f, 1.6f), new Color(1.8f, 1.8f, 1.8f), 0.1f);

        input.interactable = true;
        input.readOnly = false;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 36; // longitud de un UUID con guiones
        input.text = string.Empty;
        input.pointSize = 22;
        input.customCaretColor = true;
        input.caretColor = TextMain;
        input.caretWidth = 2;
        input.selectionColor = new Color(1f, 1f, 1f, 0.25f);

        // Margen interior para que el texto no pegue al borde
        if (input.textViewport)
        {
            Stretch(input.textViewport);
            input.textViewport.offsetMin = new Vector2(16f, 6f);
            input.textViewport.offsetMax = new Vector2(-16f, -6f);
        }
        if (input.textComponent) StyleFieldText(input.textComponent, TextMain, 22, FontStyles.Normal);
        if (input.placeholder is TMP_Text ph)
        {
            ph.enabled = true;
            StyleFieldText(ph, TextSoft, 20, FontStyles.Italic);
        }
        SetBorder(input.gameObject, FieldBorder);
    }

    // Texto de una sola línea alineado a la izquierda (texto escrito y placeholder del campo)
    static void StyleFieldText(TMP_Text t, Color color, float size, FontStyles style)
    {
        t.color = color;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
    }

    static void StyleButton(Button button)
    {
        var img = button.GetComponent<Image>();
        img.color = ButtonBg;
        img.pixelsPerUnitMultiplier = 1f;
        SetColors(button, new Color(1.6f, 1.6f, 1.6f), new Color(0.8f, 0.8f, 0.8f), new Color(1.3f, 1.3f, 1.3f), 0.12f);

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

    // Panel más alto que ancho a la izquierda: título arriba, campo y botón debajo
    static void LayoutPanel(RectTransform panel, TMP_Text title, TMP_InputField input, Button button)
    {
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = Center;
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
        if (input) Place((RectTransform)input.transform, Center, new Vector2(0f, 10f), new Vector2(340f, 52f));
        if (button) Place((RectTransform)button.transform, Center, new Vector2(0f, -80f), new Vector2(220f, 58f));
    }

    // ---------------------------------------------------------------- ventana emergente

    static void BuildPopup(Canvas canvas, RectTransform panel, Button buscar, TMP_InputField input, TMP_FontAsset font)
    {
        // Capa a pantalla completa: un velo gris que apaga el fondo y bloquea los clics a lo de detrás
        var popupRt = NewUI(PopupName, canvas.transform);
        Stretch(popupRt);
        var dim = popupRt.gameObject.AddComponent<Image>();
        dim.color = new Color(0.10f, 0.10f, 0.12f, 0.78f);
        dim.raycastTarget = true;
        popupRt.gameObject.AddComponent<CanvasGroup>();
        var popup = popupRt.gameObject.AddComponent<PopupAlumno>();

        // Ventana central
        var ventana = NewUI("Ventana", popupRt);
        Place(ventana, Center, Vector2.zero, new Vector2(460f, 460f));
        AddSlicedImage(ventana.gameObject, "UI/Skin/Background.psd", new Color(0.05f, 0.05f, 0.07f, 0.95f));
        SetBorder(ventana.gameObject, new Color(1f, 1f, 1f, 0.18f));

        // Hueco donde se dibuja el patronus
        var imgRt = NewUI("Imagen", ventana);
        Place(imgRt, Center, new Vector2(0f, -14f), new Vector2(360f, 360f));
        var img = imgRt.gameObject.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.06f);
        img.preserveAspect = true;
        img.raycastTarget = false;
        SetBorder(imgRt.gameObject, new Color(1f, 1f, 1f, 0.15f));

        // Texto que se muestra debajo del patronus (PopupAlumno lo usa para el nombre)
        var vacio = NewText("TextoVacio", imgRt, "Imagen", 22, TextSoft, font);
        vacio.fontStyle = FontStyles.Italic;

        // Botón X arriba a la derecha
        var xRt = NewUI("Cerrar", ventana);
        Place(xRt, Vector2.one, new Vector2(-10f, -10f), new Vector2(34f, 34f));
        var xImg = AddSlicedImage(xRt.gameObject, "UI/Skin/UISprite.psd", ButtonBg);
        var xBtn = xRt.gameObject.AddComponent<Button>();
        xBtn.targetGraphic = xImg;
        SetColors(xBtn, new Color(2.2f, 0.9f, 0.9f), new Color(0.8f, 0.8f, 0.8f), Color.white, 0.1f); // rojizo al pasar el ratón
        SetBorder(xRt.gameObject, Border);
        NewText("X", xRt, "X", 20, TextMain, font);

        // Referencias del popup; el menú de detrás se atenúa con su CanvasGroup
        popup.ventana = ventana;
        popup.imagen = img;
        popup.textoVacio = vacio.gameObject;
        popup.menuDetras = GetOrAdd<CanvasGroup>(panel.gameObject);
        UnityEventTools.AddPersistentListener(xBtn.onClick, new UnityAction(popup.Close));

        // Buscar no abre el popup directamente: lo abre ConsultaAlumno al recibir la respuesta de la API
        var consulta = GetOrAdd<ConsultaAlumno>(buscar.gameObject);
        consulta.popup = popup; // se reasigna porque el popup se reconstruye cada vez
        consulta.inputId = input;
        for (int i = buscar.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(buscar.onClick, i);
        UnityEventTools.AddPersistentListener(buscar.onClick, new UnityAction(consulta.AlPulsarBuscar));
        EditorUtility.SetDirty(consulta);

        popupRt.SetAsLastSibling();
        popupRt.gameObject.SetActive(false); // oculto hasta que llegue una respuesta
    }
}
