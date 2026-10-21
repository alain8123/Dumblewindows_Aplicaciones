// -----------------------------------------------------------------------------
// Archivo:  PopupBase.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using System.Collections;       // IEnumerator, necesario para la corrutina de la animación.
using UnityEngine;              // API principal de Unity.
using UnityEngine.EventSystems; // EventSystem, para quitar el foco de los botones al abrir la ventana.

/// <summary>
/// Clase base de todas las ventanas emergentes: PopupAlumno, PopupAviso y PopupSeleccionAlumno.
/// Contiene el comportamiento común:
/// <br/>- Son "modales": al abrirse quedan por encima de todo y bloquean el menú de detrás.
/// <br/>- Oscurecen el menú de detrás mientras están abiertas.
/// <br/>- Aparecen y desaparecen con un pequeño fundido y un efecto de zoom.
/// Es "abstract": no se usa directamente, sino a través de las clases que heredan de ella.
/// </summary>
[RequireComponent(typeof(CanvasGroup))] // Unity añade un CanvasGroup automáticamente; se usa para la transparencia y los clics.
public abstract class PopupBase : MonoBehaviour
{
    [SerializeField, Tooltip("Recuadro de la ventana que hace el efecto de zoom.")] private RectTransform ventana;
    [SerializeField, Tooltip("Menú que se oscurece y bloquea mientras la ventana está abierta.")] private CanvasGroup menuDetras;
    [SerializeField, Tooltip("Opacidad del menú de detrás con la ventana abierta (0 = invisible, 1 = normal).")] [Range(0f, 1f)] private float alphaMenuDetras = 0.35f;

    const float Duracion = 0.15f; // Segundos que dura la animación de abrir o cerrar.
    CanvasGroup grupo;            // CanvasGroup de esta ventana: controla su transparencia y si recibe clics.

    /// <summary>
    /// Unity lo llama una vez al crear o activar el objeto por primera vez. Guarda el CanvasGroup.
    /// "protected virtual" permite que las clases hijas lo amplíen si lo necesitan.
    /// </summary>
    protected virtual void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Muestra la ventana con animación. Lo llaman los métodos Open de cada ventana hija.
    /// </summary>
    protected void Abrir()
    {
        gameObject.SetActive(true);                     // Activa la ventana (si estaba desactivada, aquí se ejecuta Awake).
        if (!grupo) grupo = GetComponent<CanvasGroup>(); // Por seguridad, por si Awake aún no se había ejecutado.
        if (!grupo) return;                             // Sin CanvasGroup no se puede animar.

        transform.SetAsLastSibling(); // La coloca la última entre sus hermanas: en UI, la última se dibuja encima de las demás.

        if (menuDetras) menuDetras.interactable = false; // Bloquea los botones del menú de detrás.

        // Quita el foco del botón que se hubiera pulsado, para que Enter/Espacio no lo vuelva a activar por detrás.
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        StopAllCoroutines();             // Cancela una animación anterior (por ejemplo, si se estaba cerrando).
        StartCoroutine(Animar(0f, 1f)); // Animación de 0 (invisible) a 1 (visible).
    }

    /// <summary>
    /// Cierra la ventana con animación. Se asigna al OnClick del botón de cerrar / aceptar.
    /// </summary>
    public void Close()
    {
        if (!isActiveAndEnabled) return;                 // Si ya está cerrada, no hace nada.
        if (!grupo) grupo = GetComponent<CanvasGroup>(); // Por seguridad, recupera el CanvasGroup.
        if (!grupo) return;                             // Sin CanvasGroup no se puede animar.

        StopAllCoroutines(); // Cancela la animación de apertura si aún estaba en marcha.

        // Animación desde la opacidad actual hasta 0 y, al terminar, desactiva el objeto.
        StartCoroutine(Animar(grupo.alpha, 0f, () => gameObject.SetActive(false)));
    }

    /// <summary>
    /// Corrutina que hace el fundido de "desde" a "hasta" (0 = oculto, 1 = visible).
    /// Usa el tiempo real (unscaledDeltaTime) para que funcione aunque el juego esté en pausa (Time.timeScale = 0).
    /// "alFinal" es una acción opcional que se ejecuta cuando termina la animación.
    /// </summary>
    /// <param name="desde">Opacidad inicial (0 = oculto, 1 = visible).</param>
    /// <param name="hasta">Opacidad final.</param>
    /// <param name="alFinal">Acción opcional que se ejecuta al terminar la animación.</param>
    /// <returns>Enumerador que Unity usa para ejecutar la corrutina.</returns>
    IEnumerator Animar(float desde, float hasta, System.Action alFinal = null)
    {
        grupo.blocksRaycasts = true; // Durante la animación la ventana captura los clics (no atraviesan al menú).

        // Repite cada frame hasta que pase el tiempo de la animación.
        for (float tiempoTranscurrido = 0f; tiempoTranscurrido < Duracion; tiempoTranscurrido += Time.unscaledDeltaTime)
        {
            // SmoothStep suaviza el movimiento: empieza y termina despacio.
            Aplicar(Mathf.SmoothStep(desde, hasta, tiempoTranscurrido / Duracion));
            yield return null; // Espera al siguiente frame.
        }

        Aplicar(hasta);                                 // Asegura el valor final exacto.
        if (ventana) ventana.localScale = Vector3.one;  // Deja la ventana a su tamaño normal.
        alFinal?.Invoke();                              // Ejecuta la acción final, si se indicó.
    }

    /// <summary>
    /// Unity lo llama al desactivar la ventana (también si otro script la desactiva directamente).
    /// Devuelve el menú de detrás a su estado normal para que nunca se quede bloqueado ni oscurecido.
    /// </summary>
    protected virtual void OnDisable()
    {
        StopAllCoroutines(); // Detiene cualquier animación pendiente.

        if (menuDetras)
        {
            menuDetras.interactable = true; // Vuelve a permitir pulsar el menú.
            menuDetras.alpha = 1f;          // Vuelve a mostrarlo con opacidad completa.
        }

        if (grupo) grupo.blocksRaycasts = false; // La ventana deja de capturar clics.
    }

    /// <summary>
    /// Aplica un punto concreto de la animación (progreso entre 0 y 1).
    /// </summary>
    /// <param name="progreso">Punto de la animación, entre 0 (oculto) y 1 (visible).</param>
    void Aplicar(float progreso)
    {
        grupo.alpha = progreso; // Transparencia de la ventana.

        // Zoom: la ventana pasa del 92 % al 100 % de su tamaño.
        if (ventana) ventana.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, progreso);

        // El menú de detrás pasa de opacidad completa (1) a la opacidad configurada.
        if (menuDetras) menuDetras.alpha = Mathf.Lerp(1f, alphaMenuDetras, progreso);
    }
}
