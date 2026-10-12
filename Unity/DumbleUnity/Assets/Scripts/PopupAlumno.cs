using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ventana emergente que muestra el patronus del alumno animado y su nombre debajo.
// Se cierra con la X (Close, heredado de PopupBase).
public class PopupAlumno : PopupBase
{
    public Image imagen;          // recuadro donde va la animación
    public GameObject textoVacio; // texto que se usa como etiqueta con el nombre del patronus

    [Header("Animación")]
    public PatronusSpriteAnimation animacion;

    GameObject instanciaPersonalizada; // prefab propio del patronus, si lo hay

    public void Open() => Abrir();

    // Prepara la animación: un prefab propio si se indica, si no el spritesheet del patronus
    public void SetPatronus(string nombre, GameObject prefabPersonalizado = null)
    {
        AsegurarAnimacion();
        if (instanciaPersonalizada) Destroy(instanciaPersonalizada);

        animacion.gameObject.SetActive(!prefabPersonalizado);
        if (prefabPersonalizado)
        {
            instanciaPersonalizada = Instantiate(prefabPersonalizado, imagen.transform);
            if (instanciaPersonalizada.transform is RectTransform rt) Llenar(rt);
        }
        else animacion.Configurar(nombre);

        PrepararEtiqueta(nombre);
    }

    // Fondo oscuro en el recuadro y, si no existe, crea el objeto con la animación dentro
    void AsegurarAnimacion()
    {
        imagen.sprite = null;
        imagen.color = new Color(0.03f, 0.04f, 0.08f, 0.90f);
        if (animacion) return;

        var go = new GameObject("PatronusSpriteAnimation", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(imagen.transform, false);
        Llenar(rt);
        rt.SetAsFirstSibling();
        animacion = go.AddComponent<PatronusSpriteAnimation>();
    }

    // Ocupa todo el recuadro de la imagen
    static void Llenar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    // Coloca el nombre del patronus en la parte de abajo del recuadro
    void PrepararEtiqueta(string nombre)
    {
        var txt = textoVacio ? textoVacio.GetComponent<TMP_Text>() : null;
        if (!txt) return;

        var rt = txt.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 14f);
        rt.sizeDelta = new Vector2(0f, 40f);

        txt.text = nombre;
        txt.fontSize = 26;
        txt.fontStyle = FontStyles.Normal;
        txt.characterSpacing = 2;
        txt.color = new Color(0.92f, 0.92f, 0.94f, 1f);
        textoVacio.SetActive(true);
        txt.transform.SetAsLastSibling(); // por encima de la animación
    }
}
