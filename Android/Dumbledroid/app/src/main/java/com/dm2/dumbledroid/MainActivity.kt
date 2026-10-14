package com.dm2.dumbledroid

// -------------------- IMPORTS --------------------
import android.content.res.Configuration                    // Info de configuración (orientación, tamaño, etc.)
import android.os.Bundle                                   // Contenedor de estado de la Activity
import android.view.View                                   // Clase base de las vistas (para callbacks de click)
import android.widget.AdapterView                          // Eventos de selección en Spinners
import android.widget.EditText                             // Campo de texto editable
import android.widget.ImageView                            // Vista para mostrar imágenes
import android.widget.TextView                             // Vista para mostrar texto
import android.widget.Toast                                // Mensajes emergentes cortos
import androidx.activity.enableEdgeToEdge                  // Habilita el modo "edge to edge"
import androidx.appcompat.app.AppCompatActivity            // Activity base compatible con versiones antiguas
import androidx.appcompat.app.AppCompatDelegate            // Permite cambiar idioma/tema globalmente
import androidx.core.os.LocaleListCompat                   // Lista de locales compatibles
import androidx.lifecycle.lifecycleScope                   // Scope de corrutinas ligado al ciclo de vida
import androidx.recyclerview.widget.LinearLayoutManager    // Layout lineal para RecyclerView
import androidx.recyclerview.widget.RecyclerView           // Lista eficiente para muchos elementos
import coil.load                                           // Extensión de Coil para cargar imágenes desde URL
import coil.size.Size                                      // Enum para definir el tamaño de imagen
import com.dm2.dumbledroid.databinding.ActivityMainBinding // Binding del layout principal
import com.google.gson.Gson                                // Serialización JSON para guardar el estado
import com.google.gson.reflect.TypeToken                   // Tipo genérico para deserializar listas
import kotlinx.coroutines.Dispatchers                      // Hilos (IO, Main, etc.)
import kotlinx.coroutines.launch                           // Lanza una corrutina
import kotlinx.coroutines.withContext                      // Cambia de contexto dentro de una corrutina
import okhttp3.OkHttpClient                                // Cliente HTTP subyacente
import retrofit2.Retrofit                                  // Cliente HTTP para consumir la API
import retrofit2.converter.gson.GsonConverterFactory       // Convertidor JSON <-> Objetos Kotlin

/**
 * Activity principal de la aplicación "Dumbledroid".
 *
 * Se encarga de:
 *  - Mostrar el menú principal con opciones de búsqueda.
 *  - Buscar estudiantes por ID o por datos (nombre/casa).
 *  - Mostrar listas de resultados y detalles de cada estudiante.
 *  - Configurar Retrofit para consumir la API.
 *  - Permitir el cambio de idioma desde un Spinner.
 *  - **Mantener el estado (pantalla y datos) al girar el dispositivo.**
 *
 * Para que la restauración funcione correctamente, en el `AndroidManifest.xml`
 * debe estar declarado `android:configChanges` con al menos
 * `orientation|screenSize|screenLayout|smallestScreenSize|keyboardHidden`.
 */
class MainActivity : AppCompatActivity() {

    // ==========================================================
    //                       PROPIEDADES
    // ==========================================================

    /** Binding del layout `activity_main.xml` para acceder a las vistas sin findViewById. */
    private lateinit var binding: ActivityMainBinding

    /** Servicio Retrofit que define los endpoints de la API. */
    private lateinit var service: ApiService_id

    /** Adaptador que vincula la lista de estudiantes con el RecyclerView. */
    private lateinit var adapter: EstudianteListAdapter

    /**
     * Indica en qué pantalla estamos ahora mismo.
     * Valores posibles: "menu", "buscar_id", "buscar_datos", "lista", "detalle".
     * Se usa para restaurar la pantalla tras una recreación.
     */
    private var pantallaActual: String = "menu"

    /** Estudiante mostrado en la pantalla de detalle (para restaurarlo al recrear la Activity). */
    private var estudianteActual: Estudiante? = null

    /** Lista de estudiantes mostrada en la pantalla de lista (para restaurarla al recrear). */
    private var listaActual: List<Estudiante> = emptyList()

    // ==========================================================
    //                    CICLO DE VIDA
    // ==========================================================

    /**
     * Método del ciclo de vida que se ejecuta al crear la Activity.
     * Inicializa binding, adaptador, idioma y Retrofit; y restaura el estado si procede.
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

        // Crea el adaptador pasándole un callback para cuando se pulse un elemento.
        adapter = EstudianteListAdapter { estudiante ->
            mostrarDetalleAlumno(estudiante)
        }

        // Configura el Spinner para el cambio de idioma.
        configurarSpinnerIdioma()

        // Configura Retrofit con la API Key y la URL base.
        setupRetrofit()

        //    Si venimos de una recreación (giro, cambio de idioma, etc.),
        //    reconstruimos la pantalla donde estábamos con sus datos.
        savedInstanceState?.let { restaurarEstado(it) }
    }

    /**
     * Se llama antes de que la Activity sea destruida para guardar su estado.
     * Guarda la pantalla actual, el estudiante, la lista y el texto de los EditText.
     *
     * @param outState Bundle donde se almacena el estado.
     */
    override fun onSaveInstanceState(outState: Bundle) {
        super.onSaveInstanceState(outState)

        // 1. Guardamos en qué pantalla estamos.
        outState.putString(KEY_PANTALLA, pantallaActual)

        // 2. Guardamos el estudiante actual (si lo hay) serializado a JSON.
        estudianteActual?.let {
            outState.putString(KEY_ESTUDIANTE, Gson().toJson(it))
        }

        // 3. Guardamos la lista actual (si no está vacía) serializada a JSON.
        if (listaActual.isNotEmpty()) {
            outState.putString(KEY_LISTA, Gson().toJson(listaActual))
        }

        // 4. Guardamos el texto que el usuario haya escrito en los EditText visibles.
        findViewById<EditText>(R.id.et_estudiante)?.let {
            outState.putString(KEY_TXT_ID, it.text.toString())
        }
        findViewById<EditText>(R.id.et_Nombre)?.let {
            outState.putString(KEY_TXT_NOMBRE, it.text.toString())
        }
        findViewById<EditText>(R.id.et_Casa)?.let {
            outState.putString(KEY_TXT_CASA, it.text.toString())
        }
    }

    // ==========================================================
    //                 RESTAURACIÓN DE ESTADO
    // ==========================================================

    /**
     * Reconstruye la pantalla y los datos guardados en [onSaveInstanceState].
     *
     * Se llama desde [onCreate] cuando `savedInstanceState` no es nulo, es decir,
     * cuando la Activity se ha recreado (por ejemplo, tras girar el dispositivo).
     *
     * @param state Bundle con el estado previamente guardado.
     */
    private fun restaurarEstado(state: Bundle) {
        // Leemos en qué pantalla estábamos (por defecto, "menu").
        val pantalla = state.getString(KEY_PANTALLA, "menu")

        // Recuperamos el estudiante y la lista desde JSON (si existen).
        state.getString(KEY_ESTUDIANTE)?.let {
            estudianteActual = Gson().fromJson(it, Estudiante::class.java)
        }
        state.getString(KEY_LISTA)?.let { json ->
            val tipo = object : TypeToken<List<Estudiante>>() {}.type
            listaActual = Gson().fromJson(json, tipo) ?: emptyList()
        }

        // Reconstruimos la pantalla correspondiente.
        when (pantalla) {
            "buscar_id" -> {
                pantallaActual = "buscar_id"
                setContentView(R.layout.buscar_id)
                // Restauramos el texto introducido en el campo ID.
                state.getString(KEY_TXT_ID)?.let {
                    findViewById<EditText>(R.id.et_estudiante).setText(it)
                }
            }

            "buscar_datos" -> {
                pantallaActual = "buscar_datos"
                setContentView(R.layout.buscar_datos)
                // Restauramos los textos introducidos en nombre y casa.
                state.getString(KEY_TXT_NOMBRE)?.let {
                    findViewById<EditText>(R.id.et_Nombre).setText(it)
                }
                state.getString(KEY_TXT_CASA)?.let {
                    findViewById<EditText>(R.id.et_Casa).setText(it)
                }
            }

            "lista" -> {
                // Vuelve a pintar la lista con los mismos datos.
                mostrarListaAlumnos(listaActual)
            }

            "detalle" -> {
                // Vuelve a pintar el detalle del estudiante.
                estudianteActual?.let { mostrarDetalleAlumno(it) }
            }

            else -> {
                // "menu" u otro caso: no hay nada que restaurar, ya estamos en el menú.
                pantallaActual = "menu"
            }
        }
    }

    // ==========================================================
    //                       NAVEGACIÓN
    // ==========================================================

    /**
     * Muestra la pantalla de búsqueda por ID.
     *
     * @param view Vista que dispara el evento.
     */
    fun mostrar_busqueda_id(view: View) {
        pantallaActual = "buscar_id"
        setContentView(R.layout.buscar_id)
    }

    /**
     * Muestra la pantalla de búsqueda por datos (nombre y casa).
     *
     * @param view Vista que dispara el evento.
     */
    fun mostrar_busqueda_datos(view: View) {
        pantallaActual = "buscar_datos"
        setContentView(R.layout.buscar_datos)
    }

    /**
     * Vuelve al menú principal restaurando el layout raíz (`activity_main.xml`).
     *
     * @param view Vista que dispara el evento.
     */
    fun mostrar_menu(view: View) {
        pantallaActual = "menu"
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
     * Guarda la lista en [listaActual] para poder restaurarla tras una recreación.
     *
     * @param lista Lista de estudiantes a mostrar.
     */
    private fun mostrarListaAlumnos(lista: List<Estudiante>) {
        // Guardamos la pantalla y los datos para poder restaurarlos.
        pantallaActual = "lista"
        listaActual = lista

        // Cargamos el layout de lista.
        setContentView(R.layout.alumno)

        val rv = findViewById<RecyclerView>(R.id.recycler_resultados)
        rv.layoutManager = LinearLayoutManager(this)
        rv.adapter = adapter
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

                withContext(Dispatchers.Main) {
                    when {
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
     * Guarda el estudiante en [estudianteActual] para poder restaurarlo al recrear.
     *
     * @param e Objeto [Estudiante] con los datos a mostrar.
     */
    private fun mostrarDetalleAlumno(e: Estudiante) {
        // Guardamos la pantalla y el estudiante para poder restaurarlos.
        pantallaActual = "detalle"
        estudianteActual = e

        // Cargamos el layout de detalle.
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
        val client = OkHttpClient.Builder()
            .addInterceptor { chain ->
                val request = chain.request().newBuilder()
                    .addHeader("X-API-Key", BuildConfig.API_KEY) // Autenticación con la API.
                    .addHeader("accept", "*/*")                  // Acepta cualquier tipo de respuesta.
                    .build()
                chain.proceed(request)
            }
            .build()

        val retrofit = Retrofit.Builder()
            .baseUrl(Constantes.Base_URL)                          // URL base de la API.
            .client(client)                                        // Cliente OkHttp configurado.
            .addConverterFactory(GsonConverterFactory.create())    // Conversor JSON <-> Kotlin.
            .build()

        service = retrofit.create(ApiService_id::class.java)
    }

    // ==========================================================
    //               CONFIGURACIÓN DEL SPINNER IDIOMA
    // ==========================================================

    /**
     * Configura el Spinner de selección de idioma.
     *
     * Idiomas soportados (en orden): español (`es`), euskera (`eu`), inglés (`en`).
     * Al seleccionar una opción distinta a la actual, se aplica el nuevo idioma
     * a toda la aplicación usando [AppCompatDelegate.setApplicationLocales].
     */
    private fun configurarSpinnerIdioma() {
        // Lista de códigos de idioma. El orden debe coincidir con el del spinner.
        val codigos = listOf("es", "eu", "en")

        // Idioma actualmente seleccionado en la app (vacío si aún no se ha elegido).
        val tags = AppCompatDelegate.getApplicationLocales().toLanguageTags()

        // Si no hay idioma elegido, usa el del móvil; si lo hay, las 2 primeras letras.
        val actual = if (tags.isEmpty()) java.util.Locale.getDefault().language else tags.take(2)

        // Posición en la lista del idioma actual (con protección por si no lo encuentra).
        val posicion = codigos.indexOf(actual).coerceAtLeast(0)

        // Marca el spinner en el idioma actual sin disparar el listener.
        binding.selectorIdioma.setSelection(posicion, false)

        // Listener para reaccionar al cambio de idioma.
        binding.selectorIdioma.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {

            // Se ejecuta cada vez que se selecciona un elemento. "pos" es la posición elegida.
            override fun onItemSelected(p: AdapterView<*>?, v: View?, pos: Int, id: Long) {
                // Solo cambia el idioma si es distinto al actual (evita bucles).
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

    // ==========================================================
    //                       CONSTANTES
    // ==========================================================

    /**
     * Claves usadas para guardar y restaurar el estado de la Activity.
     * Se agrupan aquí para evitar errores tipográficos al escribir strings sueltos.
     */
    private companion object {
        const val KEY_PANTALLA   = "pantalla_actual"
        const val KEY_ESTUDIANTE = "estudiante_json"
        const val KEY_LISTA      = "lista_json"
        const val KEY_TXT_ID     = "txt_id"
        const val KEY_TXT_NOMBRE = "txt_nombre"
        const val KEY_TXT_CASA   = "txt_casa"
    }
}