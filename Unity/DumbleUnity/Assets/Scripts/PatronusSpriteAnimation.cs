using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PatronusSpriteAnimation : MonoBehaviour
{
    const int FrameCount = 8;
    const int FrameSize = 64;

    Image image;
    Sprite[] frames;
    Texture2D texture;
    float elapsed;
    int frameIndex;

    [SerializeField, Min(1f)] float framesPerSecond = 10f;

    void Awake()
    {
        AsegurarImagen();
    }

    bool AsegurarImagen()
    {
        if (!image) image = GetComponent<Image>();
        if (!image)
        {
            Debug.LogError("[PatronusSpriteAnimation] Falta el componente Image.", this);
            return false;
        }

        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return true;
    }

    public void Configurar(string nombre)
    {
        if (!AsegurarImagen()) return;

        LiberarFrames();
        image.sprite = null;
        string resourceName = "PatronusPixelSprites/" + NormalizarNombre(nombre) + "_pixel_8f";
        texture = Resources.Load<Texture2D>(resourceName);

        if (!texture)
        {
            Debug.LogWarning("[PatronusSpriteAnimation] No se encontró el spritesheet: " + resourceName);
            image.sprite = null;
            return;
        }

        int width = texture.width;
        int height = texture.height;
        int cellWidth = width / FrameCount;
        int cellHeight = height;
        if (cellWidth <= 0 || cellHeight <= 0 || width % FrameCount != 0)
        {
            image.sprite = null;
            return;
        }

        frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            Rect rect = new Rect(i * cellWidth, 0f, cellWidth, cellHeight);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), FrameSize, 0,
                SpriteMeshType.FullRect);
            frames[i].name = resourceName + "_frame_" + i;
        }

        frameIndex = 0;
        elapsed = 0f;
        image.sprite = frames[0];
    }

    void Update()
    {
        if (!image || frames == null || frames.Length == 0) return;

        elapsed += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        if (elapsed < frameDuration) return;

        int advances = Mathf.Min(4, Mathf.FloorToInt(elapsed / frameDuration));
        elapsed -= advances * frameDuration;
        frameIndex = (frameIndex + advances) % frames.Length;
        image.sprite = frames[frameIndex];
    }

    void OnDestroy()
    {
        LiberarFrames();
    }

    void LiberarFrames()
    {
        if (frames == null) return;
        foreach (Sprite frame in frames)
            if (frame) Destroy(frame);
        frames = null;
        texture = null;
    }

    static string NormalizarNombre(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character)) builder.Append(character);
            else if (builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
        }

        return builder.ToString().Trim('_');
    }
}
