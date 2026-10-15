using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Genera únicamente el prefab nuevo; nunca reescribe escenas ni prefabs existentes.
[InitializeOnLoad]
public static class PopupSeleccionAlumnoBuilder
{
    private const string RutaPrefab = "Assets/Resources/UI/PopupSeleccionAlumno.prefab";
    private const string Informe = "Library/PatronusSelectionSetup.txt";
    private static readonly Color Plata = new Color(0.82f, 0.85f, 0.90f, 1f);

    static PopupSeleccionAlumnoBuilder()
    {
        File.WriteAllText(Informe, "PENDING: playing=" + EditorApplication.isPlayingOrWillChangePlaymode + ", compiling=" + EditorApplication.isCompiling + ", updating=" + EditorApplication.isUpdating);
        EditorApplication.update += PrepararCuandoDisponible;
    }

    private static void PrepararCuandoDisponible()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= PrepararCuandoDisponible;
        Preparar();
    }

    [MenuItem("Tools/UI/Preparar selector de alumnos %&u")]
    public static void Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        try
        {
            VerificarLectura();
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab)) CrearPrefab();
            VerificarPrefab();
            File.WriteAllText(Informe, "OK: compilacion completada, 10 comprobaciones JSON y prefab verificado. Escenas y prefabs anteriores intactos.");
        }
        catch (Exception error)
        {
            File.WriteAllText(Informe, "ERROR: " + error.Message);
            Debug.LogException(error);
        }
    }

    private static void CrearPrefab()
    {
        Directory.CreateDirectory("Assets/Resources/UI");
        AssetDatabase.Refresh();
        GameObject raiz = new GameObject("PopupSeleccionAlumno", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        try
        {
            RectTransform fondo = (RectTransform)raiz.transform;
            Estirar(fondo);
            raiz.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            PopupSeleccionAlumno selector = raiz.AddComponent<PopupSeleccionAlumno>();

            RectTransform ventana = Rectangulo("Ventana", fondo, new Vector2(640f, 600f), Vector2.zero);
            ventana.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.03f, 0.045f, 1f);
            Outline marco = ventana.gameObject.AddComponent<Outline>();
            marco.effectColor = Plata;
            marco.effectDistance = new Vector2(2f, -2f);

            TMP_Text titulo = Texto("Titulo", ventana, "Selecciona un estudiante", new Vector2(570f, 50f), new Vector2(0f, 245f), 28f);
            Texto("Ayuda", ventana, "Hay varias coincidencias. Elige quién quieres consultar.", new Vector2(570f, 65f), new Vector2(0f, 185f), 21f);

            RectTransform zona = Rectangulo("Lista", ventana, new Vector2(560f, 340f), new Vector2(0f, -15f));
            zona.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 1f);
            ScrollRect scroll = zona.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            RectTransform viewport = Rectangulo("Viewport", zona, Vector2.zero, Vector2.zero);
            Estirar(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform contenido = Rectangulo("Contenido", viewport, Vector2.zero, Vector2.zero);
            contenido.anchorMin = new Vector2(0f, 1f);
            contenido.anchorMax = Vector2.one;
            contenido.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup distribucion = contenido.gameObject.AddComponent<VerticalLayoutGroup>();
            distribucion.padding = new RectOffset(8, 8, 8, 8);
            distribucion.spacing = 10f;
            distribucion.childControlWidth = true;
            distribucion.childControlHeight = true;
            distribucion.childForceExpandWidth = true;
            distribucion.childForceExpandHeight = false;
            ContentSizeFitter ajuste = contenido.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = contenido;

            Button plantilla = Boton("OpcionAlumno", contenido, "Nombre\nID", new Vector2(530f, 82f), Vector2.zero);
            LayoutElement tamano = plantilla.gameObject.AddComponent<LayoutElement>();
            tamano.preferredHeight = 82f;
            tamano.minHeight = 82f;
            plantilla.gameObject.SetActive(false);
            Button cancelar = Boton("Cancelar", ventana, "Cancelar", new Vector2(230f, 52f), new Vector2(0f, -245f));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(cancelar.onClick, selector.Close);

            SerializedObject campos = new SerializedObject(selector);
            campos.FindProperty("ventana").objectReferenceValue = ventana;
            campos.FindProperty("titulo").objectReferenceValue = titulo;
            campos.FindProperty("contenido").objectReferenceValue = contenido;
            campos.FindProperty("plantillaOpcion").objectReferenceValue = plantilla;
            campos.FindProperty("desplazamiento").objectReferenceValue = scroll;
            campos.ApplyModifiedPropertiesWithoutUndo();
            raiz.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(raiz);
        }
    }

    private static RectTransform Rectangulo(string nombre, Transform padre, Vector2 tamano, Vector2 posicion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.layer = LayerMask.NameToLayer("UI");
        RectTransform rectangulo = (RectTransform)objeto.transform;
        rectangulo.SetParent(padre, false);
        rectangulo.anchorMin = rectangulo.anchorMax = new Vector2(0.5f, 0.5f);
        rectangulo.sizeDelta = tamano;
        rectangulo.anchoredPosition = posicion;
        return rectangulo;
    }

    private static void Estirar(RectTransform rectangulo)
    {
        rectangulo.anchorMin = Vector2.zero;
        rectangulo.anchorMax = Vector2.one;
        rectangulo.offsetMin = rectangulo.offsetMax = Vector2.zero;
    }

    private static TMP_Text Texto(string nombre, Transform padre, string valor, Vector2 tamano, Vector2 posicion, float fuente)
    {
        RectTransform rectangulo = Rectangulo(nombre, padre, tamano, posicion);
        TextMeshProUGUI texto = rectangulo.gameObject.AddComponent<TextMeshProUGUI>();
        GameObject aviso = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PopupAviso.prefab");
        TMP_Text original = aviso ? aviso.GetComponentInChildren<TMP_Text>(true) : null;
        texto.font = original ? original.font : TMP_Settings.defaultFontAsset;
        texto.text = valor;
        texto.fontSize = fuente;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Plata;
        texto.raycastTarget = false;
        texto.richText = false;
        return texto;
    }

    private static Button Boton(string nombre, Transform padre, string etiqueta, Vector2 tamano, Vector2 posicion)
    {
        RectTransform rectangulo = Rectangulo(nombre, padre, tamano, posicion);
        Image imagen = rectangulo.gameObject.AddComponent<Image>();
        imagen.color = new Color(0.14f, 0.16f, 0.20f, 1f);
        Button boton = rectangulo.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        ColorBlock colores = boton.colors;
        colores.highlightedColor = new Color(0.8f, 0.86f, 1f, 1f);
        colores.pressedColor = new Color(0.5f, 0.6f, 0.8f, 1f);
        boton.colors = colores;
        TMP_Text texto = Texto("Texto", rectangulo, etiqueta, Vector2.zero, Vector2.zero, 23f);
        Estirar(texto.rectTransform);
        texto.margin = new Vector4(12f, 4f, 12f, 4f);
        return boton;
    }

    // Verificaciones sin consultas de red ni necesidad de entrar en Play.
    private static void VerificarLectura()
    {
        RespuestaApi[] lista = RespuestaAlumnosJson.Leer("[{\"id\":\"GR-JAM-POT\",\"nombre\":\"James\",\"apellido\":\"Potter\"},{\"id\":\"GR-JAM-POT-02\",\"nombre\":\"James Sirius\",\"apellido\":\"Potter\"}]");
        Comprobar(lista.Length == 2, "Lista ambigua");
        Comprobar(RespuestaAlumnosJson.BuscarPorId(lista, "GR-JAM-POT").nombre == "James", "ID base exacto");
        Comprobar(RespuestaAlumnosJson.BuscarPorId(lista, "GR-JAM-POT-02").nombre == "James Sirius", "ID con sufijo exacto");
        Comprobar(RespuestaAlumnosJson.BuscarPorId(lista, "GR-JAM-POT-03") == null, "ID inexistente");
        RespuestaApi[] detalle = RespuestaAlumnosJson.Leer("{\"id\":\"GR-JAM-POT-02\",\"patronus\":null}");
        // JsonUtility puede representar un string null como vacío; ambos indican que no hay Patronus.
        Comprobar(detalle.Length == 1 && string.IsNullOrWhiteSpace(detalle[0].patronus), "Detalle sin Patronus");
        RespuestaApi[] detalleVacio = RespuestaAlumnosJson.Leer("{\"id\":\"GR-JAM-POT-02\",\"patronus\":\"\"}");
        Comprobar(detalleVacio.Length == 1 && string.IsNullOrWhiteSpace(detalleVacio[0].patronus), "Detalle con Patronus vacío");
        RespuestaApi[] detalleSinCampo = RespuestaAlumnosJson.Leer("{\"id\":\"GR-JAM-POT-02\"}");
        Comprobar(detalleSinCampo.Length == 1 && string.IsNullOrWhiteSpace(detalleSinCampo[0].patronus), "Detalle sin campo Patronus");
        Comprobar(RespuestaAlumnosJson.Leer("[]").Length == 0, "Lista vacía");
        Comprobar(RespuestaAlumnosJson.Leer("{\"id\":\"GR-JAM-POT\",\"patronus\":\"Stag\"}")[0].patronus == "Stag", "Detalle con Patronus");
        bool rechazado = false;
        try { RespuestaAlumnosJson.Leer("<html>Error</html>"); }
        catch (ArgumentException) { rechazado = true; }
        Comprobar(rechazado, "Respuesta no JSON");
    }

    private static void VerificarPrefab()
    {
        GameObject recurso = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        Comprobar(recurso && recurso.GetComponent<PopupSeleccionAlumno>(), "Componente del selector");
        SerializedObject campos = new SerializedObject(recurso.GetComponent<PopupSeleccionAlumno>());
        foreach (string nombre in new[] { "ventana", "titulo", "contenido", "plantillaOpcion", "desplazamiento" })
            Comprobar(campos.FindProperty(nombre).objectReferenceValue, "Referencia: " + nombre);
    }

    private static void Comprobar(bool condicion, string caso)
    {
        if (!condicion) throw new InvalidOperationException("Ha fallado: " + caso);
    }
}
