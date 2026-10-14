using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ventana emergente que muestra el patronus del alumno animado y su nombre debajo.
// Se cierra con la X (Close, heredado de PopupBase).
public class PopupAlumno : PopupBase
{
    [SerializeField] private Image imagen;          // recuadro donde va la animación
    [SerializeField] private GameObject textoVacio; // texto que se usa como etiqueta con el nombre del patronus

    [Header("Animación")]
    [SerializeField] private PatronusSpriteAnimation animacion;

    GameObject instanciaPersonalizada; // prefab propio del patronus, si lo hay

    public void Open() => Abrir();

    // Prepara la animación: un prefab propio si se indica, si no el spritesheet del patronus
    public void SetPatronus(string nombre, GameObject prefabPersonalizado = null)
    {
        if (!AsegurarAnimacion()) return;
        if (instanciaPersonalizada)
        {
            instanciaPersonalizada.SetActive(false);
            Destroy(instanciaPersonalizada);
            instanciaPersonalizada = null;
        }

        animacion.gameObject.SetActive(!prefabPersonalizado);
        if (prefabPersonalizado)
        {
            instanciaPersonalizada = Instantiate(prefabPersonalizado, imagen.transform);
            if (instanciaPersonalizada.transform is RectTransform rectangulo) Llenar(rectangulo);
        }
        else animacion.Configurar(nombre);

        PrepararEtiqueta(nombre);
    }

    // Fondo oscuro en el recuadro y, si no existe, crea el objeto con la animación dentro
    bool AsegurarAnimacion()
    {
        // Puede llamarse antes de Awake si la ventana está inactiva.
        if (!imagen)
        {
            Debug.LogError("[PopupAlumno] Falta asignar la imagen del Patronus.", this);
            return false;
        }
        imagen.sprite = null;
        imagen.color = new Color(0.03f, 0.04f, 0.08f, 0.90f);
        if (animacion) return true;

        var objetoAnimacion = new GameObject("PatronusSpriteAnimation", typeof(RectTransform), typeof(Image));
        objetoAnimacion.layer = gameObject.layer;
        var rectangulo = (RectTransform)objetoAnimacion.transform;
        rectangulo.SetParent(imagen.transform, false);
        Llenar(rectangulo);
        rectangulo.SetAsFirstSibling();
        animacion = objetoAnimacion.AddComponent<PatronusSpriteAnimation>();
        return animacion;
    }

    // Ocupa todo el recuadro de la imagen
    static void Llenar(RectTransform rectangulo)
    {
        rectangulo.anchorMin = Vector2.zero;
        rectangulo.anchorMax = Vector2.one;
        rectangulo.offsetMin = rectangulo.offsetMax = Vector2.zero;
        rectangulo.localScale = Vector3.one;
    }

    // Coloca el nombre del patronus en la parte de abajo del recuadro
    void PrepararEtiqueta(string nombre)
    {
        var etiqueta = textoVacio ? textoVacio.GetComponent<TMP_Text>() : null;
        if (!etiqueta) return;

        var rectangulo = etiqueta.rectTransform;
        rectangulo.anchorMin = Vector2.zero;
        rectangulo.anchorMax = new Vector2(1f, 0f);
        rectangulo.pivot = new Vector2(0.5f, 0f);
        rectangulo.anchoredPosition = new Vector2(0f, 14f);
        rectangulo.sizeDelta = new Vector2(0f, 40f);

        etiqueta.text = nombre;
        etiqueta.fontSize = 26;
        etiqueta.fontStyle = FontStyles.Normal;
        etiqueta.characterSpacing = 2;
        etiqueta.color = new Color(0.92f, 0.92f, 0.94f, 1f);
        textoVacio.SetActive(true);
        etiqueta.transform.SetAsLastSibling(); // por encima de la animación
    }
}
