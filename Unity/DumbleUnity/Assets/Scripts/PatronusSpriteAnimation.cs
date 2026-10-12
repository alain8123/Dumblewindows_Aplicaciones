using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Reproduce en bucle el spritesheet pixel art de un patronus dentro de un Image de la UI.
// Los spritesheets están en Resources/PatronusPixelSprites/<nombre>_pixel_8f.png:
// 8 frames en una tira horizontal (el tamaño de cada frame se calcula a partir de la imagen).
[RequireComponent(typeof(Image))]
public class PatronusSpriteAnimation : MonoBehaviour
{
    const int FrameCount = 8;
    const int PixelsPerUnit = 64;

    [SerializeField, Min(1f)] float framesPerSecond = 10f;

    Image image;
    Sprite[] frames;
    float elapsed;
    int frameIndex;

    void Awake() => AsegurarImagen();

    // Busca el Image y lo deja listo para mostrar los frames tal cual (sin estirarlos ni recibir clics)
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

    // Carga el spritesheet del patronus indicado (ej. "Polar Bear" -> polar_bear_pixel_8f) y lo trocea en frames
    public void Configurar(string nombre)
    {
        if (!AsegurarImagen()) return;

        LiberarFrames();
        image.sprite = null;
        string resourceName = "PatronusPixelSprites/" + NormalizarNombre(nombre) + "_pixel_8f";
        var texture = Resources.Load<Texture2D>(resourceName);
        if (!texture)
        {
            Debug.LogWarning("[PatronusSpriteAnimation] No se encontró el spritesheet: " + resourceName);
            return;
        }

        int cellWidth = texture.width / FrameCount;
        if (cellWidth <= 0 || texture.height <= 0 || texture.width % FrameCount != 0) return;

        frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            var rect = new Rect(i * cellWidth, 0f, cellWidth, texture.height);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[i].name = resourceName + "_frame_" + i;
        }

        frameIndex = 0;
        elapsed = 0f;
        image.sprite = frames[0];
    }

    // Avanza de frame según el tiempo real (no le afecta Time.timeScale)
    void Update()
    {
        if (!image || frames == null || frames.Length == 0) return;

        elapsed += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        if (elapsed < frameDuration) return;

        int advances = Mathf.Min(4, Mathf.FloorToInt(elapsed / frameDuration)); // tras un tirón, como mucho salta 4
        elapsed -= advances * frameDuration;
        frameIndex = (frameIndex + advances) % frames.Length;
        image.sprite = frames[frameIndex];
    }

    void OnDestroy() => LiberarFrames();

    // Destruye los sprites creados en Configurar (la textura la gestiona Resources)
    void LiberarFrames()
    {
        if (frames == null) return;
        foreach (Sprite frame in frames)
            if (frame) Destroy(frame);
        frames = null;
    }

    // "Polar Bear" / " polar-bear " / "Cérvido" -> "polar_bear" / "polar_bear" / "cervido"
    // (minúsculas, sin tildes y con "_" en lugar de espacios y signos)
    public static string NormalizarNombre(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue; // tilde suelta
            if (char.IsLetterOrDigit(c)) builder.Append(c);
            else if (builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
        }
        return builder.ToString().Trim('_');
    }
}
