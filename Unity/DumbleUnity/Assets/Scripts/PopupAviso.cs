// -----------------------------------------------------------------------------
// Archivo:  PopupAviso.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using TMPro;       // TMP_Text, para los textos de la ventana.
using UnityEngine; // API principal de Unity.

/// <summary>
/// Ventana emergente para avisos y errores ("Alumno no encontrado", "Sin patronus", etc.).
/// Muestra un título y un mensaje. Se cierra con el botón Aceptar, que llama a Close (heredado de PopupBase).
/// </summary>
public class PopupAviso : PopupBase
{
    [SerializeField, Tooltip("Texto grande de la cabecera del aviso.")] private TMP_Text titulo;
    [SerializeField, Tooltip("Texto con la explicación del aviso.")] private TMP_Text mensaje;

    /// <summary>
    /// Rellena los textos y abre la ventana con la animación de PopupBase.
    /// </summary>
    /// <param name="textoTitulo">Título del aviso.</param>
    /// <param name="textoMensaje">Texto explicativo del aviso.</param>
    public void Open(string textoTitulo, string textoMensaje)
    {
        if (titulo) titulo.text = textoTitulo;    // Escribe el título (si el texto está asignado).
        if (mensaje) mensaje.text = textoMensaje; // Escribe el mensaje (si el texto está asignado).
        Abrir();                                  // Muestra la ventana.
    }
}
