using TMPro;

// Ventana emergente de aviso (ID no válida, no encontrada, sin patronus...).
// Se cierra con el botón Aceptar (Close, heredado de PopupBase).
public class PopupAviso : PopupBase
{
    public TMP_Text titulo;
    public TMP_Text mensaje;

    public void Open(string textoTitulo, string textoMensaje)
    {
        if (titulo) titulo.text = textoTitulo;
        if (mensaje) mensaje.text = textoMensaje;
        Abrir();
    }
}
