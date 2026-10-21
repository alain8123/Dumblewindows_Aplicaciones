// -----------------------------------------------------------------------------
// Archivo:  BotonSalir.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using UnityEngine;              // API principal de Unity.
using UnityEngine.EventSystems; // Interfaces para detectar cuándo el ratón entra o sale del botón.

/// <summary>
/// Botón X de la esquina inferior derecha: cierra la aplicación.
/// Al pasar el ratón por encima crece un poco y se inclina, como un sello que se levanta.
/// IPointerEnterHandler / IPointerExitHandler hacen que Unity avise cuando el ratón entra y sale del botón.
/// </summary>
public class BotonSalir : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f), Tooltip("Tamaño con el ratón encima (1.12 = un 12 % más grande).")] private float escalaHover = 1.12f;
    [SerializeField, Tooltip("Grados que gira con el ratón encima (negativo = sentido horario).")] private float giroHover = -6f;
    [SerializeField, Min(1f), Tooltip("Rapidez de la transición (más alto = más rápido).")] float velocidad = 8f;

    bool encima;         // true mientras el ratón está sobre el botón.
    float progresoHover; // Progreso del efecto: 0 = estado normal, 1 = efecto completo.

    /// <summary>
    /// Cierra la aplicación. Se asigna al OnClick del botón.
    /// </summary>
    public void Salir()
    {
#if UNITY_EDITOR
        // Dentro del editor de Unity, Application.Quit no hace nada: en su lugar se detiene el modo Play.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // En la aplicación ya compilada, cierra el programa.
        Application.Quit();
#endif
    }

    /// <summary>
    /// Unity lo llama cuando el ratón entra en el botón.
    /// </summary>
    /// <param name="eventData">Datos del evento del ratón (no se usan).</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        encima = true;
    }

    /// <summary>
    /// Unity lo llama cuando el ratón sale del botón.
    /// </summary>
    /// <param name="eventData">Datos del evento del ratón (no se usan).</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        encima = false;
    }

    /// <summary>
    /// Al desactivar el botón, lo devuelve a su estado normal al instante.
    /// </summary>
    void OnDisable()
    {
        encima = false;                               // Ya no hay ratón encima.
        progresoHover = 0f;                           // Sin efecto.
        transform.localScale = Vector3.one;           // Tamaño normal.
        transform.localRotation = Quaternion.identity; // Sin giro.
    }

    /// <summary>
    /// Unity lo llama cada frame. Acerca poco a poco el efecto a su objetivo
    /// (1 si el ratón está encima, 0 si no) y aplica el tamaño y el giro correspondientes.
    /// </summary>
    void Update()
    {
        // Mueve el progreso hacia el objetivo a velocidad constante. Usa tiempo real para funcionar también en pausa.
        progresoHover = Mathf.MoveTowards(progresoHover, encima ? 1f : 0f, velocidad * Time.unscaledDeltaTime);

        // SmoothStep suaviza el movimiento: arranca y termina despacio.
        float suavizadoHover = Mathf.SmoothStep(0f, 1f, progresoHover);

        // Interpola el tamaño entre normal (1) y escalaHover.
        transform.localScale = Vector3.one * Mathf.Lerp(1f, escalaHover, suavizadoHover);

        // Interpola el giro entre 0 y giroHover grados sobre el eje Z (el de la pantalla).
        transform.localRotation = Quaternion.Euler(0f, 0f, giroHover * suavizadoHover);
    }
}
