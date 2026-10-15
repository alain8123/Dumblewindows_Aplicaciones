using TMPro;
using UnityEngine;

// Ventana emergente de aviso (Nombre no encontrado, sin patronus...).
// Se cierra con el botón Aceptar (Close, heredado de PopupBase).
public class PopupAviso : PopupBase
{
    [SerializeField] private TMP_Text titulo;
    [SerializeField] private TMP_Text mensaje;

    public void Open(string textoTitulo, string textoMensaje)
    {
        if (titulo) titulo.text = textoTitulo;
        if (mensaje) mensaje.text = textoMensaje;
        Abrir();
    }
}
