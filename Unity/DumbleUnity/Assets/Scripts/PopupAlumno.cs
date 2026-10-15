// -----------------------------------------------------------------------------
// Archivo:  PopupAlumno.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using TMPro;          // TMP_Text, para la etiqueta con el nombre del patronus.
using UnityEngine;    // API principal de Unity.
using UnityEngine.UI; // Image, el recuadro donde se dibuja la animación.

/// <summary>
/// Ventana emergente que muestra el patronus del estudiante: una animación en bucle y, debajo,
/// el nombre del patronus. Se cierra con la X, que llama a Close (heredado de PopupBase).
/// La animación puede ser:
/// <br/>- Un prefab propio (si se configuró en "Animaciones personalizadas" de ConsultaAlumno), o
/// <br/>- El spritesheet del patronus, reproducido por PatronusSpriteAnimation.
/// </summary>
public class PopupAlumno : PopupBase
{
    [SerializeField, Tooltip("Recuadro de la ventana donde va la animación.")] private Image imagen;
    [SerializeField, Tooltip("Objeto con un TMP_Text que se usa como etiqueta con el nombre del patronus.")] private GameObject textoVacio;

    [Header("Animación")] // Título en el inspector.
    [SerializeField, Tooltip("Reproductor del spritesheet (si falta, se crea por código).")] private PatronusSpriteAnimation animacion;

    GameObject instanciaPersonalizada; // Copia del prefab propio que se está mostrando ahora (si hay alguna).

    /// <summary>
    /// Muestra la ventana con la animación de PopupBase. Se llama después de SetPatronus.
    /// </summary>
    public void Open()
    {
        Abrir();
    }

    /// <summary>
    /// Prepara la animación y la etiqueta del patronus indicado.
    /// "prefabPersonalizado" es opcional: si se pasa, se usa en lugar del spritesheet.
    /// </summary>
    /// <param name="nombre">Nombre del patronus, p. ej. "Stag".</param>
    /// <param name="prefabPersonalizado">Prefab propio que sustituye al spritesheet; null para usar el spritesheet.</param>
    public void SetPatronus(string nombre, GameObject prefabPersonalizado = null)
    {
        if (!AsegurarAnimacion()) return; // Sin recuadro de imagen no se puede mostrar nada.

        // Si quedaba un prefab propio de una consulta anterior, se elimina.
        if (instanciaPersonalizada)
        {
            instanciaPersonalizada.SetActive(false); // Se oculta al momento (Destroy actúa al final del frame).
            Destroy(instanciaPersonalizada);          // Se elimina.
            instanciaPersonalizada = null;            // Se olvida la referencia.
        }

        // El reproductor de spritesheets solo se ve si NO hay prefab propio.
        animacion.gameObject.SetActive(!prefabPersonalizado);

        if (prefabPersonalizado)
        {
            // Crea el prefab propio dentro del recuadro de la imagen.
            instanciaPersonalizada = Instantiate(prefabPersonalizado, imagen.transform);

            // Si es un elemento de UI, se estira para ocupar todo el recuadro.
            if (instanciaPersonalizada.transform is RectTransform rectangulo) Llenar(rectangulo);
        }
        else
        {
            animacion.Configurar(nombre); // Carga el spritesheet del patronus y empieza a animarlo.
        }

        PrepararEtiqueta(nombre); // Escribe el nombre del patronus debajo de la animación.
    }

    /// <summary>
    /// Pone el fondo oscuro del recuadro y, si no existe el reproductor de
    /// spritesheets, lo crea por código dentro del recuadro. Devuelve false si falta la imagen.
    /// </summary>
    /// <returns>false si falta asignar la imagen; true si todo está listo.</returns>
    bool AsegurarAnimacion()
    {
        // Puede llamarse antes de que Unity ejecute Awake si la ventana está desactivada,
        // por eso no se da nada por hecho y se comprueba todo aquí.
        if (!imagen)
        {
            Debug.LogError("[PopupAlumno] Falta asignar la imagen del Patronus.", this);
            return false;
        }

        imagen.sprite = null;                                // El recuadro no muestra ninguna imagen propia...
        imagen.color = new Color(0.03f, 0.04f, 0.08f, 0.90f); // ...solo un fondo casi negro y un poco transparente.

        if (animacion) return true; // El reproductor ya existe: listo.

        // Crea un objeto de UI nuevo con Image (donde se pintarán los frames).
        var objetoAnimacion = new GameObject("PatronusSpriteAnimation", typeof(RectTransform), typeof(Image));
        objetoAnimacion.layer = gameObject.layer; // Misma capa que la ventana, para que la misma cámara lo dibuje.

        var rectangulo = (RectTransform)objetoAnimacion.transform; // Su RectTransform (posición y tamaño en la UI).
        rectangulo.SetParent(imagen.transform, false);             // Lo mete dentro del recuadro de la imagen.
        Llenar(rectangulo);                                        // Ocupa todo el recuadro.
        rectangulo.SetAsFirstSibling();                            // Primero entre sus hermanos: se dibuja debajo de la etiqueta.

        animacion = objetoAnimacion.AddComponent<PatronusSpriteAnimation>(); // Añade el reproductor de spritesheets.
        return animacion; // true si se ha creado bien.
    }

    /// <summary>
    /// Hace que un elemento de UI ocupe exactamente todo el espacio de su padre.
    /// </summary>
    /// <param name="rectangulo">Elemento de UI que debe ocupar todo su padre.</param>
    static void Llenar(RectTransform rectangulo)
    {
        rectangulo.anchorMin = Vector2.zero;                       // Esquina inferior izquierda anclada a la del padre.
        rectangulo.anchorMax = Vector2.one;                        // Esquina superior derecha anclada a la del padre.
        rectangulo.offsetMin = rectangulo.offsetMax = Vector2.zero; // Sin márgenes.
        rectangulo.localScale = Vector3.one;                       // Escala normal (por si el prefab traía otra).
    }

    /// <summary>
    /// Coloca el texto con el nombre del patronus en la parte baja del recuadro y le da estilo.
    /// </summary>
    /// <param name="nombre">Nombre del patronus que se escribe en la etiqueta.</param>
    void PrepararEtiqueta(string nombre)
    {
        // Obtiene el componente de texto del objeto "textoVacio" (null si no está asignado).
        var etiqueta = textoVacio ? textoVacio.GetComponent<TMP_Text>() : null;
        if (!etiqueta) return; // Sin texto no hay etiqueta que mostrar.

        // Posición: franja de 40 px de alto pegada al borde inferior, con 14 px de separación.
        var rectangulo = etiqueta.rectTransform;
        rectangulo.anchorMin = Vector2.zero;                // Anclada abajo a la izquierda...
        rectangulo.anchorMax = new Vector2(1f, 0f);         // ...y abajo a la derecha (ocupa todo el ancho).
        rectangulo.pivot = new Vector2(0.5f, 0f);           // Punto de referencia: centro del borde inferior.
        rectangulo.anchoredPosition = new Vector2(0f, 14f); // 14 px por encima del borde inferior.
        rectangulo.sizeDelta = new Vector2(0f, 40f);        // Mismo ancho que el padre y 40 px de alto.

        // Estilo del texto.
        etiqueta.text = nombre;                                 // Nombre del patronus.
        etiqueta.fontSize = 26;                                 // Tamaño de letra.
        etiqueta.fontStyle = FontStyles.Normal;                 // Sin negrita ni cursiva.
        etiqueta.characterSpacing = 2;                          // Un poco de espacio entre letras.
        etiqueta.color = new Color(0.92f, 0.92f, 0.94f, 1f);    // Blanco ligeramente grisáceo.

        textoVacio.SetActive(true);              // Muestra la etiqueta.
        etiqueta.transform.SetAsLastSibling();   // Último entre sus hermanos: se dibuja por encima de la animación.
    }
}
