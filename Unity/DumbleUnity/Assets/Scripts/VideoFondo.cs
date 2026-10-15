// -----------------------------------------------------------------------------
// Archivo:  VideoFondo.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using UnityEngine;       // API principal de Unity.
using UnityEngine.UI;    // RawImage, el componente que muestra el vídeo en la UI.
using UnityEngine.Video; // VideoPlayer, el reproductor de vídeo de Unity.

/// <summary>
/// Muestra a pantalla completa, en este RawImage, el vídeo de fondo (el castillo animado).
/// Cómo funciona:
/// <br/>1. El VideoPlayer prepara el vídeo (carga y lee su tamaño).
/// <br/>2. Se crea una RenderTexture (una imagen que se puede actualizar en tiempo real) del tamaño del vídeo.
/// <br/>3. El VideoPlayer pinta cada fotograma en esa textura y el RawImage la muestra.
/// Funciona tanto en modo Play como en el editor, para ver el fondo mientras se diseña la escena.
/// </summary>
[ExecuteAlways]                       // Ejecuta el script también en el editor, sin pulsar Play.
[RequireComponent(typeof(RawImage))]  // Unity añade un RawImage automáticamente si no lo tiene.
public class VideoFondo : MonoBehaviour
{
    [SerializeField, Tooltip("Reproductor con el vídeo (si no se asigna, se busca en la escena).")] private VideoPlayer player;

    RawImage imagenVideo;       // Componente donde se muestra el vídeo.
    RenderTexture texturaVideo; // Textura donde el VideoPlayer pinta cada fotograma.

    /// <summary>
    /// Unity lo llama al activar el objeto. Configura el reproductor y empieza a preparar el vídeo.
    /// Cuando el vídeo está listo, Unity llama a OnPrepared.
    /// </summary>
    void OnEnable()
    {
        imagenVideo = GetComponent<RawImage>();                 // RawImage de este mismo objeto.
        if (!player) player = FindAnyObjectByType<VideoPlayer>(); // Sin reproductor asignado: busca uno en la escena.

        // Sin RawImage, sin reproductor o sin vídeo asignado no se puede reproducir nada.
        if (!imagenVideo || !player || !player.clip)
        {
            Debug.LogError("[VideoFondo] No hay VideoPlayer con clip asignado.");
            return;
        }

        player.renderMode = VideoRenderMode.RenderTexture; // El vídeo se pintará en una textura (no directamente en pantalla).
        player.isLooping = true;                           // Vuelve a empezar al terminar.

        // Sonido solo en modo Play; en el editor se silencia para no molestar mientras se trabaja.
        player.audioOutputMode = Application.isPlaying ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;

        player.errorReceived += OnError;        // Si hay un error de vídeo, se llamará a OnError.
        player.prepareCompleted += OnPrepared;  // Cuando el vídeo esté listo, se llamará a OnPrepared.
        player.Prepare();                       // Empieza a cargar el vídeo (no bloquea la app).
    }

    /// <summary>
    /// El vídeo ya está cargado y se conoce su tamaño.
    /// Crea la textura, la conecta al RawImage y arranca la reproducción.
    /// </summary>
    /// <param name="reproductor">VideoPlayer que ha terminado de preparar el vídeo.</param>
    void OnPrepared(VideoPlayer reproductor)
    {
        // Si el objeto se desactivó mientras se preparaba, o el vídeo no tiene tamaño, no se hace nada.
        if (!isActiveAndEnabled || !reproductor || !imagenVideo || reproductor.width == 0 || reproductor.height == 0) return;

        if (!texturaVideo) // Solo se crea la primera vez.
        {
            texturaVideo = new RenderTexture((int)reproductor.width, (int)reproductor.height, 0) // Mismo tamaño que el vídeo, sin profundidad.
            {
                name = "VideoFondoRT",           // Nombre para identificarla al depurar.
                hideFlags = HideFlags.DontSave   // No se guarda en la escena (se crea de nuevo en cada ejecución).
            };
            texturaVideo.Create(); // Reserva la textura en la tarjeta gráfica.
        }

        reproductor.targetTexture = texturaVideo; // El vídeo se pinta en la textura...
        imagenVideo.texture = texturaVideo;        // ...y el RawImage muestra esa textura.
        imagenVideo.color = Color.white;           // Sin tinte (al desactivar se pone en negro).
        reproductor.Play();                        // Empieza la reproducción.
    }

    /// <summary>
    /// Escribe en la consola cualquier error que avise el reproductor de vídeo.
    /// </summary>
    /// <param name="reproductor">VideoPlayer que ha producido el error.</param>
    /// <param name="mensaje">Descripción del error.</param>
    void OnError(VideoPlayer reproductor, string mensaje)
    {
        Debug.LogError("[VideoFondo] " + mensaje);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Fuera del modo Play, Unity apenas redibuja la escena.
    /// Esto le pide que la actualice continuamente para que el vídeo se vea en movimiento en las vistas Scene y Game.
    /// </summary>
    void Update()
    {
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    /// <summary>
    /// Unity lo llama al desactivar el objeto. Se da de baja de los eventos,
    /// desconecta la textura y la libera para no dejar memoria ocupada.
    /// </summary>
    void OnDisable()
    {
        if (player)
        {
            player.prepareCompleted -= OnPrepared;                               // Deja de escuchar "vídeo preparado".
            player.errorReceived -= OnError;                                     // Deja de escuchar errores.
            if (!Application.isPlaying) player.Stop();                           // En el editor, detiene el vídeo.
            if (player.targetTexture == texturaVideo) player.targetTexture = null; // Desconecta la textura del reproductor.
        }

        if (imagenVideo)
        {
            imagenVideo.texture = null;      // El RawImage deja de mostrar la textura...
            imagenVideo.color = Color.black; // ...y se queda en negro.
        }

        if (texturaVideo)
        {
            texturaVideo.Release(); // Libera la memoria de la tarjeta gráfica.

            // Destruye el objeto textura: Destroy en modo Play, DestroyImmediate en el editor (allí Destroy no está permitido).
            if (Application.isPlaying) Destroy(texturaVideo);
            else DestroyImmediate(texturaVideo);

            texturaVideo = null; // Olvida la referencia.
        }
    }
}
