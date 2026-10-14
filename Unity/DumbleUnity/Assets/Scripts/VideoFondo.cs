using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Muestra un VideoPlayer en este RawImage a pantalla completa, tanto en Play como en el editor.
// Crea la RenderTexture con el tamaño real del vídeo.
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class VideoFondo : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;

    RawImage imagenVideo;
    RenderTexture texturaVideo; // textura donde el VideoPlayer pinta cada fotograma

    // Prepara el vídeo; cuando está listo se llama a OnPrepared
    void OnEnable()
    {
        imagenVideo = GetComponent<RawImage>();
        if (!player) player = FindAnyObjectByType<VideoPlayer>();
        if (!imagenVideo || !player || !player.clip)
        {
            Debug.LogError("[VideoFondo] No hay VideoPlayer con clip asignado.");
            return;
        }

        player.renderMode = VideoRenderMode.RenderTexture;
        player.isLooping = true;
        player.audioOutputMode = Application.isPlaying ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None; // sin sonido en el editor
        player.errorReceived += OnError;
        player.prepareCompleted += OnPrepared;
        player.Prepare();
    }

    // Ya se conoce el tamaño del vídeo: crea la textura, la muestra en el RawImage y arranca la reproducción
    void OnPrepared(VideoPlayer reproductor)
    {
        if (!isActiveAndEnabled || !reproductor || !imagenVideo || reproductor.width == 0 || reproductor.height == 0) return;
        if (!texturaVideo)
        {
            texturaVideo = new RenderTexture((int)reproductor.width, (int)reproductor.height, 0) { name = "VideoFondoRT", hideFlags = HideFlags.DontSave };
            texturaVideo.Create();
        }
        reproductor.targetTexture = texturaVideo;
        imagenVideo.texture = texturaVideo;
        imagenVideo.color = Color.white;
        reproductor.Play();
    }

    void OnError(VideoPlayer reproductor, string mensaje) => Debug.LogError("[VideoFondo] " + mensaje);

#if UNITY_EDITOR
    void Update()
    {
        // En modo edición fuerza a refrescar para que el vídeo se mueva en la vista Scene/Game
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    // Suelta los eventos y libera la textura (en el editor también para el vídeo)
    void OnDisable()
    {
        if (player)
        {
            player.prepareCompleted -= OnPrepared;
            player.errorReceived -= OnError;
            if (!Application.isPlaying) player.Stop();
            if (player.targetTexture == texturaVideo) player.targetTexture = null;
        }
        if (imagenVideo) { imagenVideo.texture = null; imagenVideo.color = Color.black; }
        if (texturaVideo)
        {
            texturaVideo.Release();
            if (Application.isPlaying) Destroy(texturaVideo);
            else DestroyImmediate(texturaVideo);
            texturaVideo = null;
        }
    }
}
