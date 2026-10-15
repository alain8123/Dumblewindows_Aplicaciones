// -----------------------------------------------------------------------------
// Archivo:  EtiquetaDerechos.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using UnityEngine;              // API principal de Unity.
using UnityEngine.EventSystems; // Interfaces para detectar cuándo el ratón entra o sale de la etiqueta.

/// <summary>
/// Etiqueta "© Dumblewindows" que muestra el recuadro de derechos de autor mientras el ratón está encima.
/// <br/>El recuadro es un objeto normal de la escena (hijo de la etiqueta, desactivado): su texto, colores
/// y posición se editan en el inspector. Este script solo lo activa y lo desactiva.
/// <br/>Importante: el texto de la etiqueta debe tener "Raycast Target" activado para detectar el ratón.
/// </summary>
public class EtiquetaDerechos : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Recuadro con el texto de derechos de autor; debe empezar desactivado.")]
    private GameObject recuadro;

    /// <summary>
    /// Unity lo llama cuando el ratón entra en la etiqueta. Muestra el recuadro.
    /// </summary>
    /// <param name="eventData">Datos del evento del ratón (no se usan).</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (recuadro) recuadro.SetActive(true);
    }

    /// <summary>
    /// Unity lo llama cuando el ratón sale de la etiqueta. Oculta el recuadro.
    /// </summary>
    /// <param name="eventData">Datos del evento del ratón (no se usan).</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (recuadro) recuadro.SetActive(false);
    }

    /// <summary>
    /// Unity lo llama al desactivar la etiqueta. Oculta el recuadro para que no se quede abierto.
    /// </summary>
    void OnDisable()
    {
        if (recuadro) recuadro.SetActive(false);
    }
}
