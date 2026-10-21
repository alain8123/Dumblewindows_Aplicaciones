// -----------------------------------------------------------------------------
// Archivo:  PatronusSpriteAnimation.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using System.Globalization; // CharUnicodeInfo y NormalizationForm, para quitar tildes al normalizar nombres.
using System.Text;          // StringBuilder, para construir el nombre normalizado.
using UnityEngine;          // API principal de Unity.
using UnityEngine.UI;       // Image, el componente donde se pinta cada frame.

/// <summary>
/// Reproduce en bucle la animación pixel art de un patronus dentro de un Image de la UI.
/// Cómo están guardadas las animaciones:
/// <br/>- Carpeta: Assets/Resources/PatronusPixelSprites/
/// <br/>- Nombre:  &lt;nombre_normalizado&gt;_pixel_8f.png   (p. ej. "Polar Bear" -&gt; polar_bear_pixel_8f.png)
/// <br/>- Formato: una tira horizontal con 8 frames del mismo ancho (actualmente 960x120 -&gt; frames de 120x120).
/// Para añadir un patronus nuevo basta con dejar su PNG en esa carpeta con el nombre correcto.
/// </summary>
[RequireComponent(typeof(Image))] // Unity añade un Image automáticamente si no lo tiene.
public class PatronusSpriteAnimation : MonoBehaviour
{
    const int FrameCount = 8;      // Número de frames que tiene cada tira.
    const int PixelsPerUnit = 64;  // Píxeles de la imagen por unidad de Unity al crear los sprites.

    [SerializeField, Min(1f), Tooltip("Velocidad de la animación (frames por segundo, mínimo 1).")] private float framesPerSecond = 10f;

    Image image;      // Componente donde se muestra el frame actual.
    Sprite[] frames;  // Los 8 sprites recortados de la tira.
    float elapsed;    // Tiempo acumulado desde el último cambio de frame.
    int frameIndex;   // Frame que se está mostrando ahora (de 0 a 7).

    /// <summary>
    /// Unity lo llama al crear el objeto. Prepara el Image.
    /// </summary>
    void Awake()
    {
        AsegurarImagen();
    }

    /// <summary>
    /// Busca el Image y lo configura para mostrar los frames tal cual
    /// (sin deformarlos y sin que capture clics). Devuelve false si no hay Image.
    /// </summary>
    /// <returns>false si el objeto no tiene componente Image.</returns>
    bool AsegurarImagen()
    {
        if (!image) image = GetComponent<Image>(); // Lo busca solo la primera vez.

        if (!image)
        {
            Debug.LogError("[PatronusSpriteAnimation] Falta el componente Image.", this);
            return false;
        }

        image.preserveAspect = true;      // Mantiene la proporción del frame (no lo estira).
        image.raycastTarget = false;      // No bloquea clics: la animación es solo decorativa.
        image.type = Image.Type.Simple;   // Dibuja el sprite completo, sin recortes ni mosaicos.
        image.color = Color.white;        // Sin tinte: se ven los colores originales del sprite.
        return true;
    }

    /// <summary>
    /// Carga la tira del patronus indicado y la divide en 8 sprites.
    /// Ejemplo: "Polar Bear" -&gt; carga Resources/PatronusPixelSprites/polar_bear_pixel_8f.
    /// </summary>
    /// <param name="nombre">Nombre del patronus, p. ej. "Polar Bear".</param>
    public void Configurar(string nombre)
    {
        if (!AsegurarImagen()) return; // Sin Image no se puede mostrar nada.

        LiberarFrames();     // Borra los sprites del patronus anterior.
        image.sprite = null; // Deja el recuadro vacío mientras se carga el nuevo.

        // Los distintos textos de "no corpóreo" comparten el mismo sprite; el resto usa su nombre normalizado.
        string nombreNormalizado = EsNoCorporeo(nombre) ? "non_corporeal" : NormalizarNombre(nombre);

        // Ruta dentro de Resources (sin la extensión .png).
        string rutaRecurso = "PatronusPixelSprites/" + nombreNormalizado + "_pixel_8f";

        var textura = Resources.Load<Texture2D>(rutaRecurso); // Carga la imagen completa.
        if (!textura)
        {
            // No existe un PNG con ese nombre: se avisa en la consola y el recuadro queda vacío.
            Debug.LogWarning("[PatronusSpriteAnimation] No se encontró el spritesheet: " + rutaRecurso);
            return;
        }

        int anchoFotograma = textura.width / FrameCount; // Ancho de cada frame (p. ej. 960 / 8 = 120).

        // Comprueba que la imagen se puede dividir exactamente en 8 frames.
        if (anchoFotograma <= 0 || textura.height <= 0 || textura.width % FrameCount != 0)
        {
            Debug.LogWarning("[PatronusSpriteAnimation] El spritesheet debe contener ocho fotogramas horizontales.", this);
            return;
        }

        frames = new Sprite[FrameCount]; // Reserva espacio para los 8 sprites.

        // Recorta cada frame de la tira y crea un sprite con él.
        for (int indiceFotograma = 0; indiceFotograma < FrameCount; indiceFotograma++)
        {
            // Rectángulo del frame dentro de la tira: (x, y, ancho, alto).
            var regionFotograma = new Rect(indiceFotograma * anchoFotograma, 0f, anchoFotograma, textura.height);

            // Crea el sprite con el pivote en el centro (0.5, 0.5) y una malla rectangular simple.
            frames[indiceFotograma] = Sprite.Create(textura, regionFotograma, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);

            // Nombre descriptivo, útil al depurar en el editor.
            frames[indiceFotograma].name = rutaRecurso + "_frame_" + indiceFotograma;
        }

        frameIndex = 0;          // Empieza por el primer frame.
        elapsed = 0f;            // Reinicia el contador de tiempo.
        image.sprite = frames[0]; // Muestra el primer frame.
    }

    /// <summary>
    /// Unity lo llama una vez por frame. Avanza la animación según el tiempo real
    /// (unscaledDeltaTime), así no le afecta Time.timeScale (por ejemplo, si el juego está en pausa).
    /// </summary>
    void Update()
    {
        if (!image || frames == null || frames.Length == 0) return; // Nada cargado: no hay que animar.

        elapsed += Time.unscaledDeltaTime;                            // Suma el tiempo transcurrido desde el último Update.
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);    // Segundos que dura cada frame (0.1 s a 10 fps).
        if (elapsed < frameDuration) return;                          // Aún no toca cambiar de frame.

        // Cuántos frames hay que avanzar. Si la app se ha atascado un momento, salta como mucho 4 para no dar un salto brusco.
        int advances = Mathf.Min(4, Mathf.FloorToInt(elapsed / frameDuration));

        elapsed -= advances * frameDuration;                 // Descuenta el tiempo ya consumido.
        frameIndex = (frameIndex + advances) % frames.Length; // Avanza y vuelve al 0 al pasar del último (bucle).
        image.sprite = frames[frameIndex];                   // Muestra el nuevo frame.
    }

    /// <summary>
    /// Unity lo llama al destruir el objeto. Libera los sprites creados.
    /// </summary>
    void OnDestroy()
    {
        LiberarFrames();
    }

    /// <summary>
    /// Destruye los sprites creados en Configurar para no acumular memoria.
    /// La textura no se destruye porque la gestiona Resources.
    /// </summary>
    void LiberarFrames()
    {
        if (frames == null) return; // No hay sprites que liberar.

        foreach (Sprite frame in frames)
            if (frame) Destroy(frame); // Destruye cada sprite que exista.

        frames = null; // Olvida el array.
    }

    /// <summary>
    /// Indica si el texto del patronus significa "patronus no corpóreo"
    /// (sin forma de animal). Reconoce varias formas de escribirlo, en inglés y en español.
    /// </summary>
    /// <param name="nombre">Texto del patronus recibido de la API.</param>
    /// <returns>true si significa "patronus no corpóreo".</returns>
    public static bool EsNoCorporeo(string nombre)
    {
        string nombreNormalizado = NormalizarNombre(nombre); // P. ej. "Non-corporeal" -> "non_corporeal".

        return nombreNormalizado == "non_corporeal"
            || nombreNormalizado == "noncorporeal"
            || nombreNormalizado == "no_corporeo"
            || nombreNormalizado == "no_corporeal"
            || nombreNormalizado == "no_corporea"
            || nombreNormalizado == "non_corporeal_patronus";
    }

    /// <summary>
    /// Convierte un nombre en el formato de los archivos de sprites.
    /// <br/>- Todo en minúsculas.
    /// <br/>- Sin tildes.
    /// <br/>- Espacios, guiones y otros signos se sustituyen por un único "_".
    /// Ejemplos: "Polar Bear" -&gt; "polar_bear" · " polar-bear " -&gt; "polar_bear" · "Cérvido" -&gt; "cervido".
    /// </summary>
    /// <param name="nombre">Nombre a normalizar.</param>
    /// <returns>El nombre en minúsculas, sin tildes y con "_" como separador.</returns>
    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return string.Empty; // Nombre vacío: resultado vacío.

        // FormD separa cada letra de su tilde ("é" -> "e" + "´"), así luego se puede quitar la tilde.
        string nombreDescompuesto = nombre.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var nombreNormalizado = new StringBuilder(nombreDescompuesto.Length); // Resultado en construcción.

        foreach (char caracter in nombreDescompuesto)
        {
            // Tilde suelta (separada por FormD): se descarta.
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(caracter))
            {
                nombreNormalizado.Append(caracter); // Letra o número: se conserva.
            }
            else if (nombreNormalizado.Length > 0 && nombreNormalizado[nombreNormalizado.Length - 1] != '_')
            {
                nombreNormalizado.Append('_'); // Cualquier otro signo se convierte en "_" (sin repetir "_" seguidos).
            }
        }

        return nombreNormalizado.ToString().Trim('_'); // Quita un posible "_" final.
    }
}
