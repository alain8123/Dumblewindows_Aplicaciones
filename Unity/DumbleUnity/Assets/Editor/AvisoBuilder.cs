using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UIUtil;

// Crea el popup de avisos (mismo estilo que PopupAlumno) y lo conecta con ConsultaAlumno.
// Tools > Menú > Crear popup de aviso. Se puede repetir: reconstruye el popup y vuelve a enlazarlo.
public static class AvisoBuilder
{
    const string AvisoName = "PopupAviso";

    static readonly Color ButtonBg = new Color(0.12f, 0.12f, 0.15f, 0.90f);
    static readonly Color Border = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color TextMain = new Color(0.92f, 0.92f, 0.94f, 1f);
    static readonly Color TextSoft = new Color(0.80f, 0.80f, 0.85f, 0.85f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

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

        // Reutilizar la fuente y el menú atenuable del popup principal
        CanvasGroup menuGroup = consulta.popup ? consulta.popup.menuDetras : null;
        TMP_Text textoPopup = consulta.popup && consulta.popup.textoVacio ? consulta.popup.textoVacio.GetComponent<TMP_Text>() : null;
        TMP_FontAsset font = textoPopup ? textoPopup.font : null;

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
        Place(ventana, Center, Vector2.zero, new Vector2(460f, 260f));
        AddSlicedImage(ventana.gameObject, "UI/Skin/Background.psd", new Color(0.05f, 0.05f, 0.07f, 0.95f));
        SetBorder(ventana.gameObject, new Color(1f, 1f, 1f, 0.18f));

        // Título arriba
        var titulo = NewText("Titulo", ventana, "Aviso", 28, TextMain, font);
        titulo.characterSpacing = 2;
        var tr = titulo.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -24f);
        tr.sizeDelta = new Vector2(-40f, 44f);

        // Mensaje en el centro (con saltos de línea automáticos)
        var mensaje = NewText("Mensaje", ventana, "", 22, TextSoft, font);
        mensaje.textWrappingMode = TextWrappingModes.Normal;
        mensaje.rectTransform.offsetMin = new Vector2(30f, 90f);
        mensaje.rectTransform.offsetMax = new Vector2(-30f, -76f);

        // Botón Aceptar abajo
        var btnRt = NewUI("Aceptar", ventana);
        Place(btnRt, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 48f));
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = AddSlicedImage(btnRt.gameObject, "UI/Skin/UISprite.psd", ButtonBg);
        SetColors(btn, new Color(1.6f, 1.6f, 1.6f), new Color(0.8f, 0.8f, 0.8f), new Color(1.3f, 1.3f, 1.3f), 0.12f);
        SetBorder(btnRt.gameObject, Border);
        NewText("Text (TMP)", btnRt, "ACEPTAR", 22, TextMain, font).characterSpacing = 2;

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

        EditorSceneManager.MarkSceneDirty(consulta.gameObject.scene);
        EditorSceneManager.SaveScene(consulta.gameObject.scene);
        Debug.Log("[AvisoBuilder] Popup de aviso creado y enlazado con ConsultaAlumno");
    }
}
