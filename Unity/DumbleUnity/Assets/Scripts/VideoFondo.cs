using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Muestra un VideoPlayer en este RawImage a pantalla completa, tanto en Play como en el editor.
// Crea la RenderTexture con el tamaño real del vídeo.
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class VideoFondo : MonoBehaviour
{
    public VideoPlayer player;

    RawImage image;
    RenderTexture rt;

    void OnEnable()
    {
        image = GetComponent<RawImage>();
        if (!player) player = FindAnyObjectByType<VideoPlayer>();
        if (!player || !player.clip)
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

    void OnPrepared(VideoPlayer p)
    {
        if (!rt)
        {
            rt = new RenderTexture((int)p.width, (int)p.height, 0) { name = "VideoFondoRT", hideFlags = HideFlags.DontSave };
            rt.Create();
        }
        p.targetTexture = rt;
        image.texture = rt;
        image.color = Color.white;
        p.Play();
    }

    void OnError(VideoPlayer p, string msg) => Debug.LogError("[VideoFondo] " + msg);

#if UNITY_EDITOR
    void Update()
    {
        // En modo edición fuerza a refrescar para que el vídeo se mueva en la vista Scene/Game
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    void OnDisable()
    {
        if (player)
        {
            player.prepareCompleted -= OnPrepared;
            player.errorReceived -= OnError;
            if (!Application.isPlaying) { player.Stop(); player.targetTexture = null; }
        }
        if (image) { image.texture = null; image.color = Color.black; }
        if (rt) { rt.Release(); DestroyImmediate(rt); rt = null; }
    }
}
