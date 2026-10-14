package com.dm2.dumbledroid

// -------------------- IMPORTS --------------------
// Se importan las clases necesarias para el funcionamiento de la Activity:
import android.content.pm.ActivityInfo
import android.content.res.Configuration          // Configuracion para saber la orientacion de la pantalla
import android.os.Bundle                          // Contenedor de estado de la Activity
import android.view.View                          // Clase base de los componentes visuales (para callbacks de clicks)
import android.widget.AdapterView                 // Maneja eventos de selección en Spinners
import android.widget.EditText                    // Campo de texto editable
import android.widget.ImageView                   // Vista para mostrar imágenes
import android.widget.TextView                    // Vista para mostrar texto
import android.widget.Toast                       // Mensajes emergentes cortos
import androidx.activity.enableEdgeToEdge         // Habilita el modo "edge to edge" (UI a pantalla completa)
import androidx.appcompat.app.AppCompatActivity   // Activity base compatible con versiones antiguas
import androidx.appcompat.app.AppCompatDelegate   // Permite cambiar idioma/tema globalmente
import androidx.core.os.LocaleListCompat          // Lista de locales compatibles
import androidx.lifecycle.lifecycleScope          // Scope de corrutinas ligado al ciclo de vida
import androidx.recyclerview.widget.LinearLayoutManager // Layout lineal para RecyclerView
import androidx.recyclerview.widget.RecyclerView  // Lista eficiente para muchos elementos
import coil.load                                  // Extensión de Coil para cargar imágenes desde URL
import com.dm2.dumbledroid.databinding.ActivityMainBinding // Binding generado del layout principal
import kotlinx.coroutines.Dispatchers             // Define hilos (IO, Main, etc.)
import kotlinx.coroutines.launch                  // Lanza una corrutina
import kotlinx.coroutines.withContext             // Cambia de contexto dentro de una corrutina
import retrofit2.Retrofit                         // Cliente HTTP para consumir la API
import retrofit2.converter.gson.GsonConverterFactory // Convertidor JSON <-> Objetos Kotlin
import okhttp3.OkHttpClient                       // Cliente HTTP subyacente
import coil.size.Size                             // Enum para definir el tamaño de la imagen

/**
 * Activity principal de la aplicación "Dumbledroid".
 *
 * Se encarga de:
 *  - Mostrar el menú principal con opciones de búsqueda.
 *  - Buscar estudiantes por ID o por datos (nombre/casa).
 *  - Mostrar listas de resultados y detalles de cada estudiante.
 *  - Configurar Retrofit para consumir la API.
 *  - Permitir el cambio de idioma desde un Spinner.
 */
class MainActivity : AppCompatActivity() {

    // -------------------- PROPIEDADES --------------------
    /** Binding del layout `activity_main.xml` para acceder a las vistas sin findViewById. */
    private lateinit var binding: ActivityMainBinding

    /** Servicio Retrofit que define los endpoints de la API. */
    private lateinit var service: ApiService_id

    /** Adaptador que vincula la lista de estudiantes con el RecyclerView. */
    private lateinit var adapter: EstudianteListAdapter

    /**
     * Método del ciclo de vida que se ejecuta al crear la Activity.
     * Inicializa el binding, el adaptador, el spinner de idioma y Retrofit.
     *
     * @param savedInstanceState Estado previo guardado (si la Activity se recrea).
     */
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Habilita el diseño "edge to edge" (contenido detrás de las barras del sistema).
        enableEdgeToEdge()

        // Infla el layout principal usando ViewBinding.
        binding = ActivityMainBinding.inflate(layoutInflater)

        // Establece la vista raíz como contenido de la Activity.
        setContentView(binding.root)

        // Crea el adaptador pasándole un lambda como callback.
        // Cuando se pulse un elemento de la lista, se llamará a mostrarDetalleAlumno().
        adapter = EstudianteListAdapter { estudiante ->
            mostrarDetalleAlumno(estudiante)
        }

        // Configura el Spinner para el cambio de idioma.
        configurarSpinnerIdioma()

        // Configura Retrofit con la API Key y la URL base.
        setupRetrofit()
    }

    // ==========================================================
    //                       NAVEGACIÓN
    // ==========================================================

    /**
     * Muestra la pantalla de búsqueda por ID.
     *
     * @param view Vista que dispara el evento (definida en el layout XML).
     */
    fun mostrar_busqueda_id(view: View) {
        // Cambia el contenido de la pantalla al layout `buscar_id.xml`.
        setContentView(R.layout.buscar_id)
    }

    /**
     * Muestra la pantalla de búsqueda por datos (nombre y casa).
     *
     * @param view Vista que dispara el evento.
     */
    fun mostrar_busqueda_datos(view: View) {
        // Cambia el contenido de la pantalla al layout `buscar_datos.xml`.
        setContentView(R.layout.buscar_datos)
    }

    /**
     * Vuelve al menú principal restaurando el layout raíz (`activity_main.xml`).
     *
     * @param view Vista que dispara el evento.
     */
    fun mostrar_menu(view: View) {
        // Restaura el layout principal usando el binding previamente inflado.
        setContentView(binding.root)
    }

    // ==========================================================
    //                   BUSCAR POR ID
    // ==========================================================

    /**
     * Recoge el ID introducido por el usuario y lanza la búsqueda.
     *
     * @param view Vista que dispara el evento (botón buscar).
     */
    fun buscar_estudiante_id(view: View) {
        // Obtiene el texto del campo `et_estudiante` y elimina espacios sobrantes.
        val id = findViewById<EditText>(R.id.et_estudiante).text.toString().trim()

        // Validación: si el campo está vacío, muestra un aviso y sale.
        if (id.isEmpty()) {
            Toast.makeText(this, "Escribe un ID", Toast.LENGTH_SHORT).show()
            return
        }

        // Informa al usuario de que la búsqueda ha comenzado.
        Toast.makeText(this, "Buscando...", Toast.LENGTH_SHORT).show()

        // Lanza la petición a la API.
        getEstudiantePorId(id)
    }

    /**
     * Realiza la petición a la API para obtener un estudiante por su ID.
     * Se ejecuta en un hilo de IO y devuelve el resultado al hilo principal.
     *
     * @param id Identificador del estudiante a buscar.
     */
    private fun getEstudiantePorId(id: String) {
        // Lanza una corrutina ligada al ciclo de vida de la Activity.
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                // Llama al endpoint del servicio pasando el ID.
                val estudiante = service.getEstudiantePorId(id = id)

                // Vuelve al hilo principal para actualizar la UI.
                withContext(Dispatchers.Main) {
                    // Muestra la pantalla de detalle con los datos obtenidos.
                    mostrarDetalleAlumno(estudiante)
                }
            } catch (e: Exception) {
                // En caso de error, vuelve al hilo principal y muestra un Toast.
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "Error: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    // ==========================================================
    //                   BUSCAR POR DATOS
    // ==========================================================

    /**
     * Recoge los datos (nombre y/o casa) y lanza la búsqueda.
     *
     * @param view Vista que dispara el evento (botón buscar).
     */
    fun buscar_estudiante_datos(view: View) {
        // Obtiene el texto de los campos nombre y casa.
        val nombre = findViewById<EditText>(R.id.et_Nombre).text.toString().trim()
        val casa   = findViewById<EditText>(R.id.et_Casa).text.toString().trim()

        // Validación: al menos uno de los dos campos debe estar relleno.
        if (nombre.isEmpty() && casa.isEmpty()) {
            Toast.makeText(this, "Rellena al menos un campo", Toast.LENGTH_SHORT).show()
            return
        }

        // Informa al usuario de que la búsqueda ha comenzado.
        Toast.makeText(this, "Buscando...", Toast.LENGTH_SHORT).show()

        // Lanza la petición a la API.
        getEstudiantePorDatos(nombre, casa)
    }

    /**
     * Muestra una lista de alumnos en un RecyclerView dentro del layout `alumno.xml`.
     *
     * @param lista Lista de estudiantes que se mostrará.
     */
    private fun mostrarListaAlumnos(lista: List<Estudiante>) {
        // Cambia el contenido de la pantalla al layout de lista.
        setContentView(R.layout.alumno)

        // Obtiene la referencia al RecyclerView del layout.
        val rv = findViewById<RecyclerView>(R.id.recycler_resultados)

        // Asigna un LinearLayoutManager (lista vertical) al RecyclerView.
        rv.layoutManager = LinearLayoutManager(this)

        // Asigna el adaptador previamente creado.
        rv.adapter = adapter

        // Envía la lista al adaptador para que la renderice.
        adapter.submitList(lista)
    }

    /**
     * Realiza la petición a la API para obtener estudiantes filtrando por nombre y/o casa.
     * Según el número de resultados:
     *  - 0 resultados: muestra un Toast de "Sin resultados".
     *  - 1 resultado: va directo a la pantalla de detalle.
     *  - Varios resultados: muestra la lista en `alumno.xml`.
     *
     * @param nombre Nombre del estudiante (puede ser vacío).
     * @param casa   Casa a la que pertenece (puede ser vacío).
     */
    private fun getEstudiantePorDatos(nombre: String, casa: String) {
        // Lanza corrutina en hilo de IO.
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                // Llama al endpoint pasando nombre y casa como filtros.
                val lista = service.getEstudiantesPorDatos(
                    name = nombre,
                    house = casa
                )

                // Vuelve al hilo principal para decidir qué mostrar.
                withContext(Dispatchers.Main) {
                    when {
                        // Caso 1: no hay resultados.
                        lista.isEmpty() -> {
                            Toast.makeText(
                                this@MainActivity,
                                "Sin resultados",
                                Toast.LENGTH_SHORT
                            ).show()
                        }
                        // Caso 2: un único resultado → detalle directo.
                        lista.size == 1 -> {
                            mostrarDetalleAlumno(lista.first())
                        }
                        // Caso 3: varios resultados → mostrar lista.
                        else -> {
                            mostrarListaAlumnos(lista)
                        }
                    }
                }
            } catch (e: Exception) {
                // En caso de error, muestra un Toast en el hilo principal.
                withContext(Dispatchers.Main) {
                    Toast.makeText(
                        this@MainActivity,
                        "Error: ${e.message}",
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    // ==========================================================
    //               PANTALLA DETALLE DEL ALUMNO
    // ==========================================================

    /**
     * Muestra la pantalla de detalle de un estudiante concreto.
     * Carga la imagen desde la URL y rellena todos los campos de texto.
     *
     * @param e Objeto [Estudiante] con los datos a mostrar.
     */
    private fun mostrarDetalleAlumno(e: Estudiante) {
        // Carga la ventana
        setContentView(R.layout.mostrar_alumno)

        // Carga la imagen desde la URL que devuelve la API usando Coil.
        findViewById<ImageView>(R.id.detalle_imagen).load(e.imagen) {
            crossfade(true)                 // Animación de transición suave.
            placeholder(R.mipmap.ejemplo)   // Imagen mientras carga.
            error(R.mipmap.ejemplo)         // Imagen si falla la carga.
            size(Size.ORIGINAL)             // Usa el tamaño original de la imagen.
        }

        // Rellena cada TextView con su dato correspondiente del estudiante.
        findViewById<TextView>(R.id.detalle_nombre).text            = e.nombre
        findViewById<TextView>(R.id.detalle_alias).text             = e.alias
        findViewById<TextView>(R.id.detalle_casa).text              = e.casa
        findViewById<TextView>(R.id.detalle_especie).text           = e.especie
        findViewById<TextView>(R.id.detalle_genero).text            = e.genero
        findViewById<TextView>(R.id.detalle_fecha_nacimiento).text  = e.fecha_nacimiento
        findViewById<TextView>(R.id.detalle_patronus).text          = e.patronus
        findViewById<TextView>(R.id.detalle_nacionalidad).text      = e.nacionalidad
    }

    // ==========================================================
    //               CONFIGURACIÓN DE RETROFIT
    // ==========================================================

    /**
     * Configura el cliente Retrofit con:
     *  - Un interceptor que añade la API Key y la cabecera `accept`.
     *  - La URL base definida en [Constantes.Base_URL].
     *  - El convertidor Gson para parsear JSON.
     */
    private fun setupRetrofit() {
        // Construye un OkHttpClient con un interceptor personalizado.
        val client = OkHttpClient.Builder()
            .addInterceptor { chain ->
                // Crea una nueva petición añadiendo las cabeceras necesarias.
                val request = chain.request().newBuilder()
                    .addHeader("X-API-Key", BuildConfig.API_KEY) // Autenticación con la API.
                    .addHeader("accept", "*/*")                  // Acepta cualquier tipo de respuesta.
                    .build()
                // Continúa la cadena con la petición modificada.
                chain.proceed(request)
            }
            .build()

        // Construye la instancia de Retrofit.
        val retrofit = Retrofit.Builder()
            .baseUrl(Constantes.Base_URL)                          // URL base de la API.
            .client(client)                                        // Cliente OkHttp configurado.
            .addConverterFactory(GsonConverterFactory.create())    // Conversor JSON <-> Kotlin.
            .build()

        // Crea el servicio a partir de la interfaz ApiService_id.
        service = retrofit.create(ApiService_id::class.java)
    }

    // ==========================================================
    //               CONFIGURACIÓN DEL SPINNER IDIOMA
    // ==========================================================

    /**
     * Configura el Spinner de selección de idioma.
     *
     * Idiomas soportados (en orden): español (`es`), euskera (`eu`), inglés (`en`).
     *
     * Al seleccionar una opción distinta a la actual, se aplica el nuevo idioma
     * a toda la aplicación usando [AppCompatDelegate.setApplicationLocales].
     */
    private fun configurarSpinnerIdioma() {
        // Lista de códigos de idioma. El orden debe coincidir con el de las opciones del spinner:
        // posición 0 = español, 1 = euskera, 2 = inglés
        val codigos = listOf("es", "eu", "en")

        // Obtiene el idioma que se ha elegido en la app (por ejemplo "eu").
        // Si el usuario aún no ha elegido ninguno, el texto viene vacío.
        val tags = AppCompatDelegate.getApplicationLocales().toLanguageTags()

        // Si no hay idioma elegido, usa el idioma del móvil; si lo hay, se queda con
        // las 2 primeras letras ("es").
        val actual = if (tags.isEmpty()) java.util.Locale.getDefault().language else tags.take(2)

        // Busca en qué posición de la lista está ese idioma (0, 1 o 2).
        // Si no lo encuentra (devuelve -1), `coerceAtLeast(0)` asegura que el valor no sea negativo.
        val posicion = codigos.indexOf(actual).coerceAtLeast(0)

        // Deja el spinner marcado en el idioma actual.
        // El "false" indica que no hay que animar ni disparar el listener al hacerlo,
        // así evitamos que se cambie el idioma solo al abrir la pantalla.
        binding.selectorIdioma.setSelection(posicion, false)

        // Define qué hacer cuando el usuario selecciona una opción del spinner.
        binding.selectorIdioma.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {

            // Se ejecuta cada vez que se selecciona un elemento. "pos" es la posición elegida.
            override fun onItemSelected(p: AdapterView<*>?, v: View?, pos: Int, id: Long) {
                // Solo cambia el idioma si el elegido es distinto al actual;
                // así no se recarga la pantalla sin necesidad (ni se entra en bucle).
                if (pos != posicion) {
                    // Aplica el nuevo idioma a toda la app. Android recrea la Activity
                    // y carga los textos del `values-xx` correspondiente.
                    AppCompatDelegate.setApplicationLocales(
                        LocaleListCompat.forLanguageTags(codigos[pos])
                    )
                }
            }

            // Se ejecuta si no hay nada seleccionado. No necesitamos hacer nada aquí.
            override fun onNothingSelected(p: AdapterView<*>?) {}
        }
    }
}