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

    [SerializeField, Min(1f)] private float framesPerSecond = 10f;

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
        string nombreNormalizado = EsNoCorporeo(nombre) ? "non_corporeal" : NormalizarNombre(nombre);
        string rutaRecurso = "PatronusPixelSprites/" + nombreNormalizado + "_pixel_8f";
        var textura = Resources.Load<Texture2D>(rutaRecurso);
        if (!textura)
        {
            Debug.LogWarning("[PatronusSpriteAnimation] No se encontró el spritesheet: " + rutaRecurso);
            return;
        }

        int anchoFotograma = textura.width / FrameCount;
        if (anchoFotograma <= 0 || textura.height <= 0 || textura.width % FrameCount != 0)
        {
            Debug.LogWarning("[PatronusSpriteAnimation] El spritesheet debe contener ocho fotogramas horizontales.", this);
            return;
        }

        frames = new Sprite[FrameCount];
        for (int indiceFotograma = 0; indiceFotograma < FrameCount; indiceFotograma++)
        {
            var regionFotograma = new Rect(indiceFotograma * anchoFotograma, 0f, anchoFotograma, textura.height);
            frames[indiceFotograma] = Sprite.Create(textura, regionFotograma, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[indiceFotograma].name = rutaRecurso + "_frame_" + indiceFotograma;
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

    // Reconoce las variantes de nombre usadas para el Patronus no corpóreo.
    // "Polar Bear" / " polar-bear " / "Cérvido" -> "polar_bear" / "polar_bear" / "cervido"
    // (minúsculas, sin tildes y con "_" en lugar de espacios y signos)
    public static bool EsNoCorporeo(string nombre)
    {
        string nombreNormalizado = NormalizarNombre(nombre);
        return nombreNormalizado == "non_corporeal" || nombreNormalizado == "noncorporeal" ||
               nombreNormalizado == "no_corporeo" || nombreNormalizado == "no_corporeal" ||
               nombreNormalizado == "no_corporea" || nombreNormalizado == "non_corporeal_patronus";
    }
    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;

        string nombreDescompuesto = nombre.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var nombreNormalizado = new StringBuilder(nombreDescompuesto.Length);
        foreach (char caracter in nombreDescompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark) continue; // tilde suelta
            if (char.IsLetterOrDigit(caracter)) nombreNormalizado.Append(caracter);
            else if (nombreNormalizado.Length > 0 && nombreNormalizado[nombreNormalizado.Length - 1] != '_') nombreNormalizado.Append('_');
        }
        return nombreNormalizado.ToString().Trim('_');
    }
}
