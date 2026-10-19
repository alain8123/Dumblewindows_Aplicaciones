package com.dm2.dumbledroid

// -------------------- IMPORTS --------------------
import android.os.Bundle                                   // Contenedor de estado de la Activity
import android.util.Log
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
import com.dm2.dumbledroid.repository.EstudianteRepository
import com.dm2.dumbledroid.utils.ErrorUtils
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
 *  - Mantener el estado (pantalla y datos) al girar el dispositivo.
 *  - Usar caché SQLite (24h) para búsquedas offline.
 */
class MainActivity : AppCompatActivity() {

    // ==========================================================
    //                       PROPIEDADES
    // ==========================================================

    /** Binding del layout `activity_main.xml`. */
    private lateinit var binding: ActivityMainBinding

    /** Servicio Retrofit con los endpoints de la API. */
    private lateinit var service: ApiService_id

    /** Adaptador de la lista de estudiantes. */
    private lateinit var adapter: EstudianteListAdapter

    /** Repositorio con caché SQLite. */
    private lateinit var repository: EstudianteRepository

    /** Pantalla actual: "menu", "buscar_id", "buscar_datos", "lista", "detalle". */
    private var pantallaActual: String = "menu"

    /** Estudiante mostrado en la pantalla de detalle (para restaurar tras recrear). */
    private var estudianteActual: Estudiante? = null

    /** Lista mostrada en la pantalla de lista (para restaurar tras recrear). */
    private var listaActual: List<Estudiante> = emptyList()

    /** Formato de ID único: XX-XXX-XXX. */
    private val REGEX_ID = Regex("^[A-Z]{2}-[A-Z]{3}-[A-Z]{3}$")

    /** Formato de ID duplicado: XX-XXX-XXX-NN. */
    private val REGEX_ID_DUPE = Regex("^[A-Z]{2}-[A-Z]{3}-[A-Z]{3}-[0-9]{2}$")

    // ==========================================================
    //                    CICLO DE VIDA
    // ==========================================================

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        enableEdgeToEdge()

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Adaptador con callback al pulsar un elemento.
        adapter = EstudianteListAdapter { estudiante ->
            mostrarDetalleAlumno(estudiante)
        }

        // Configuración inicial.
        configurarSpinnerIdioma()
        setupRetrofit()

        // Repositorio con caché SQLite.
        repository = EstudianteRepository(this, service)
        repository.limpiarCacheCaducada()

        // Restaurar estado si venimos de una recreación.
        savedInstanceState?.let { restaurarEstado(it) }

        // Refresca el botón "último consultado" por si ya hay algo en caché.
        actualizarBotonUltimoConsultado()
    }

    override fun onSaveInstanceState(outState: Bundle) {
        super.onSaveInstanceState(outState)

        outState.putString(KEY_PANTALLA, pantallaActual)

        estudianteActual?.let {
            outState.putString(KEY_ESTUDIANTE, Gson().toJson(it))
        }
        if (listaActual.isNotEmpty()) {
            outState.putString(KEY_LISTA, Gson().toJson(listaActual))
        }

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

    private fun restaurarEstado(state: Bundle) {
        val pantalla = state.getString(KEY_PANTALLA, "menu")

        state.getString(KEY_ESTUDIANTE)?.let {
            estudianteActual = Gson().fromJson(it, Estudiante::class.java)
        }
        state.getString(KEY_LISTA)?.let { json ->
            val tipo = object : TypeToken<List<Estudiante>>() {}.type
            listaActual = Gson().fromJson(json, tipo) ?: emptyList()
        }

        when (pantalla) {
            "buscar_id" -> {
                pantallaActual = "buscar_id"
                setContentView(R.layout.buscar_id)
                state.getString(KEY_TXT_ID)?.let {
                    findViewById<EditText>(R.id.et_estudiante).setText(it)
                }
            }
            "buscar_datos" -> {
                pantallaActual = "buscar_datos"
                setContentView(R.layout.buscar_datos)
                state.getString(KEY_TXT_NOMBRE)?.let {
                    findViewById<EditText>(R.id.et_Nombre).setText(it)
                }
                state.getString(KEY_TXT_CASA)?.let {
                    findViewById<EditText>(R.id.et_Casa).setText(it)
                }
            }
            "lista" -> mostrarListaAlumnos(listaActual)
            "detalle" -> estudianteActual?.let { mostrarDetalleAlumno(it) }
            else -> pantallaActual = "menu"
        }
    }

    // ==========================================================
    //                       NAVEGACIÓN
    // ==========================================================

    fun mostrar_busqueda_id(view: View) {
        pantallaActual = "buscar_id"
        setContentView(R.layout.buscar_id)
    }

    fun mostrar_busqueda_datos(view: View) {
        pantallaActual = "buscar_datos"
        setContentView(R.layout.buscar_datos)
    }

    /**
     * Vuelve al menú principal y refresca el botón "último consultado".
     */
    fun mostrar_menu(view: View) {
        pantallaActual = "menu"
        setContentView(binding.root)
        actualizarBotonUltimoConsultado()
    }

    // ==========================================================
    //                   BUSCAR POR ID
    // ==========================================================

    fun buscar_estudiante_id(view: View) {
        val id = findViewById<EditText>(R.id.et_estudiante).text.toString().trim().uppercase()

        if (id.isEmpty()) {
            Toast.makeText(this, getString(R.string.escribe_un_id), Toast.LENGTH_SHORT).show()
            return
        }

        if (verificar_formato_id(id)) {
            Toast.makeText(this, getString(R.string.buscando), Toast.LENGTH_SHORT).show()
            getEstudiantesPorId(id)
        } else {
            Toast.makeText(
                this,
                getString(R.string.el_id_no_cumple_el_formato),
                Toast.LENGTH_SHORT
            ).show()
        }
    }

    private fun verificar_formato_id(id: String): Boolean {
        return REGEX_ID.matches(id.trim()) || REGEX_ID_DUPE.matches(id.trim())
    }

    private fun getEstudiantesPorId(id: String) {
        val base = idBase(id)

        mostrarCargando()

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val lista = repository.getEstudiantesPorId(base, id)

                withContext(Dispatchers.Main) {
                    when {
                        lista.isEmpty() -> Toast.makeText(
                            this@MainActivity,
                            getString(R.string.sin_resultados),
                            Toast.LENGTH_SHORT
                        ).show()

                        lista.size == 1 -> mostrarDetalleAlumno(lista.first())

                        else -> mostrarListaAlumnos(lista)
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {

                    ocultarCargando()

                    // Mensaje amigable en lugar del e.message crudo
                    val mensaje = ErrorUtils.mensajeAmigable(this@MainActivity, e)

                    Toast.makeText(
                        this@MainActivity,
                        mensaje,
                        Toast.LENGTH_LONG
                    ).show()

                    Log.d("Error busqueda por id", e.message.toString())
                }
            }
        }
    }

    private fun idBase(id: String): String {
        val partes = id.split("-")
        return if (partes.size == 4 && partes[3].length == 2 && partes[3].all { it.isDigit() }) {
            partes.take(3).joinToString("-")
        } else {
            id
        }
    }

    // ==========================================================
    //                   BUSCAR POR DATOS
    // ==========================================================

    fun buscar_estudiante_datos(view: View) {
        val nombre = findViewById<EditText>(R.id.et_Nombre).text.toString().trim()
        val casa = findViewById<EditText>(R.id.et_Casa).text.toString().trim()

        // Validación de nombre: letras, espacios y apóstrofos.
        val regexNombre = Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\\s']+$")
        if (nombre.isNotEmpty() && !regexNombre.matches(nombre)) {
            Toast.makeText(
                this,
                getString(R.string.el_nombre_solo_puede_contener_letras_espacios_y_ap_strofos),
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        // Validación de casa: solo letras y espacios.
        val regexCasa = Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\\s]+$")
        if (casa.isNotEmpty() && !regexCasa.matches(casa)) {
            Toast.makeText(
                this,
                getString(R.string.la_casa_solo_puede_contener_letras_y_espacios),
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        if (nombre.isEmpty() && casa.isEmpty()) {
            Toast.makeText(
                this,
                getString(R.string.rellena_al_menos_un_campo),
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        Toast.makeText(this, R.string.buscando, Toast.LENGTH_SHORT).show()
        getEstudiantePorDatos(nombre, casa)
    }

    private fun mostrarListaAlumnos(lista: List<Estudiante>) {
        pantallaActual = "lista"
        listaActual = lista

        setContentView(R.layout.alumno)

        val rv = findViewById<RecyclerView>(R.id.recycler_resultados)
        rv.layoutManager = LinearLayoutManager(this)
        rv.adapter = adapter
        adapter.submitList(lista)
    }

    private fun getEstudiantePorDatos(nombre: String, casa: String) {

        mostrarCargando()

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val lista = repository.getEstudiantesPorDatos(nombre, casa)

                withContext(Dispatchers.Main) {
                    when {
                        lista.isEmpty() -> Toast.makeText(
                            this@MainActivity,
                            getString(R.string.sin_resultados),
                            Toast.LENGTH_SHORT
                        ).show()

                        lista.size == 1 -> mostrarDetalleAlumno(lista.first())

                        else -> mostrarListaAlumnos(lista)
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {

                    // Mensaje amigable en lugar del e.message crudo
                    val mensaje = ErrorUtils.mensajeAmigable(this@MainActivity, e)

                    Toast.makeText(
                        this@MainActivity,
                        mensaje,
                        Toast.LENGTH_LONG
                    ).show()

                    ocultarCargando()

                    Log.d("Error busqueda por datos", e.message.toString())
                }
            }
        }
    }

    // ==========================================================
    //               PANTALLA DETALLE DEL ALUMNO
    // ==========================================================

    /**
     * Muestra la pantalla de detalle de un estudiante.
     *
     * Además, marca este estudiante como "el último consultado" en la caché,
     * para que el botón del menú principal lo muestre al volver.
     *
     * @param e Estudiante a mostrar.
     */
    private fun mostrarDetalleAlumno(e: Estudiante) {
        pantallaActual = "detalle"
        estudianteActual = e

        // Marca este estudiante como el último consultado.
        // Esto actualiza su timestamp en SQLite y hace que el botón
        // "Último: ..." del menú principal lo muestre al volver.
        repository.marcarComoConsultado(e)
        Log.d("Dumbledroid", "Marcado como consultado: ${e.id}")

        setContentView(R.layout.mostrar_alumno)

        // Imagen (si es null, Coil mostrará el placeholder/error).
        findViewById<ImageView>(R.id.detalle_imagen).load(e.imagen.orEmpty()) {
            crossfade(true)
            placeholder(R.mipmap.ejemplo)
            error(R.mipmap.ejemplo)
            size(Size.ORIGINAL)
        }

        // Nombre completo (nombre + apellido), tratados con orEmpty().
        val nombreCompleto = listOfNotNull(e.nombre, e.apellido)
            .joinToString(" ")
            .trim()

        findViewById<TextView>(R.id.detalle_nombre).text           = e.nombre + " " + e.apellido
        findViewById<TextView>(R.id.detalle_alias).text            = e.alias.orEmpty()
        findViewById<TextView>(R.id.detalle_casa).text             = e.casa.orEmpty()
        findViewById<TextView>(R.id.detalle_especie).text          = e.especie.orEmpty()
        findViewById<TextView>(R.id.detalle_genero).text           = e.genero.orEmpty()
        findViewById<TextView>(R.id.detalle_fecha_nacimiento).text = e.fecha_nacimiento.orEmpty()
        findViewById<TextView>(R.id.detalle_patronus).text         = e.patronus.orEmpty()
        findViewById<TextView>(R.id.detalle_nacionalidad).text     = e.nacionalidad.orEmpty()
    }

    // ==========================================================
    //               CONFIGURACIÓN DE RETROFIT
    // ==========================================================

    private fun setupRetrofit() {
        val client = OkHttpClient.Builder()
            .addInterceptor { chain ->
                val request = chain.request().newBuilder()
                    .addHeader("X-API-Key", BuildConfig.API_KEY)
                    .addHeader("accept", "*/*")
                    .build()
                chain.proceed(request)
            }
            .build()

        val retrofit = Retrofit.Builder()
            .baseUrl(Constantes.Base_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
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

    fun boton_ayuda_id(view: View) {
        val titulo = getString(R.string.ayuda_formato_del_id)
        val texto = getString(R.string.el_formato_del_id_es) + "\n" +
                "XX-XXX-XXX" + "\n" +
                getString(R.string.las_2_primeras_x_son_las_iniciales_de_la_casa) + "\n" +
                getString(R.string.las_3_primeras_x_son_las_iniciales_del_nombre) + "\n" +
                getString(R.string.las_3_ultimas_x_son_las_iniciales_del_apellido) + "\n" +
                getString(R.string.por_ejemplo_el_estudiante_harry_james_potter_seria) + "\n" +
                "GR-HAR-POT" + "\n" + "\n" +
                getString(R.string.estudiante_con_id_repetido)

        MiDialogoFragment.newInstance(titulo, texto)
            .show(supportFragmentManager, "miDialogo")
    }

    fun boton_ayuda_datos(view: View) {
        val titulo = getString(R.string.ayuda_de_busqueda_por_datos)
        val texto =
            getString(R.string.al_buscar_por_datos_introducir_el_campo_de_la_casa_es_opcional) + "\n" +
                    getString(R.string.buscara_la_coincidencia_de_los_caracteres) + "\n" + "\n" +
                    getString(R.string.ejemplo) + "\n" + "\n" +
                    getString(R.string.al_buscar_ivan_te_muestra) + "\n" + "\n" +
                    "Medb O'Sullivan, Sillivan Fawley, ..."

        MiDialogoFragment.newInstance(titulo, texto)
            .show(supportFragmentManager, "miDialogo")
    }

    // ==========================================================
    //                       ÚLTIMO CONSULTADO
    // ==========================================================

    override fun onResume() {
        super.onResume()
        if (pantallaActual == "menu") {
            actualizarBotonUltimoConsultado()
        }
    }

    /**
     * Refresca el botón "último consultado".
     * Ahora usa el ID marcado explícitamente en vez del timestamp.
     */
    private fun actualizarBotonUltimoConsultado() {
        val btn = binding.btMenuUltimo
        val ultimo = repository.getUltimoConsultado()

        if (ultimo != null) {
            btn.visibility = View.VISIBLE
            val nombre = listOfNotNull(ultimo.nombre, ultimo.apellido)
                .joinToString(" ")
                .trim()
            btn.text = "Último: $nombre"
            Log.d("Dumbledroid", "Último consultado: ${ultimo.id} - $nombre")
        } else {
            btn.visibility = View.GONE
            Log.d("Dumbledroid", "No hay último consultado")
        }
    }

    fun abrirUltimoConsultado(view: View) {
        val ultimo = repository.getUltimoConsultado()
        if (ultimo != null) {
            mostrarDetalleAlumno(ultimo)
        } else {
            Toast.makeText(this, "No hay ningún estudiante reciente", Toast.LENGTH_SHORT).show()
        }
    }

    // ==========================================================
    //                       PANTALLA DE CARGA
    // ==========================================================

    /**
     * Muestra el indicador de carga en la pantalla actual.
     * El ProgressBar se busca por ID en el layout visible; si no existe,
     * simplemente no hace nada (para pantallas sin ProgressBar).
     */
    private fun mostrarCargando() {
        findViewById<android.widget.ProgressBar>(R.id.progress_cargando)?.visibility = View.VISIBLE
        // Deshabilita el botón de buscar mientras carga.
        findViewById<View>(R.id.bt_Buscar)?.isEnabled = false
    }

    /**
     * Oculta el indicador de carga en la pantalla actual.
     */
    private fun ocultarCargando() {
        findViewById<android.widget.ProgressBar>(R.id.progress_cargando)?.visibility = View.GONE
        findViewById<View>(R.id.bt_Buscar)?.isEnabled = true
    }

    // ==========================================================
    //                       CONSTANTES
    // ==========================================================

    private companion object {
        const val KEY_PANTALLA   = "pantalla_actual"
        const val KEY_ESTUDIANTE = "estudiante_json"
        const val KEY_LISTA      = "lista_json"
        const val KEY_TXT_ID     = "txt_id"
        const val KEY_TXT_NOMBRE = "txt_nombre"
        const val KEY_TXT_CASA   = "txt_casa"
    }
}