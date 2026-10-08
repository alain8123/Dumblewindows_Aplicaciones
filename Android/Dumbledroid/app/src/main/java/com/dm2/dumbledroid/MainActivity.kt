package com.dm2.dumbledroid

import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat
import androidx.lifecycle.lifecycleScope
import coil.load
import com.dm2.dumbledroid.databinding.ActivityMainBinding
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.create
import okhttp3.OkHttpClient
import com.dm2.dumbledroid.BuildConfig
import coil.size.Size

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var service: ApiService_id

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        configurarSpinnerIdioma()
        setupRetrofit()
    }

    // ---------- Navegación ----------

    fun mostrar_busqueda_id(view: View) {
        setContentView(R.layout.buscar_id)
    }

    fun mostrar_busqueda_datos(view: View) {
        setContentView(R.layout.buscar_datos)
    }

    fun mostrar_menu(view: View) {
        setContentView(binding.root)
    }

    // ---------- Buscar por ID ----------

    fun buscar_estudiante_id(view: View) {
        val id = findViewById<EditText>(R.id.et_estudiante).text.toString().trim()
        if (id.isEmpty()) {
            Toast.makeText(this, "Escribe un ID", Toast.LENGTH_SHORT).show()
            return
        }
        Toast.makeText(this, "Buscando...", Toast.LENGTH_SHORT).show()
        getEstudiantePorId(id)   // ← descomenta esto
    }

    private fun getEstudiantePorId(id: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val estudiante = service.getEstudiantePorId(id = id)
                withContext(Dispatchers.Main) {
                    mostrarDetalleAlumno(estudiante)
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "Error: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }

    fun buscar_estudiante_datos(view: View) {
        val nombre = findViewById<EditText>(R.id.et_Nombre).text.toString().trim()
        val casa   = findViewById<EditText>(R.id.et_Casa).text.toString().trim()

        if (nombre.isEmpty() && casa.isEmpty()) {
            Toast.makeText(this, "Rellena al menos un campo", Toast.LENGTH_SHORT).show()
            return
        }

        Toast.makeText(this, "Buscando...", Toast.LENGTH_SHORT).show()
        getEstudiantePorDatos(nombre, casa)
    }

    private fun getEstudiantePorDatos(nombre: String, casa: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val lista = service.getEstudiantesPorDatos(
                    name = nombre,
                    house = casa
                )

                withContext(Dispatchers.Main) {
                    if (lista.isEmpty()) {
                        Toast.makeText(
                            this@MainActivity,
                            "Sin resultados",
                            Toast.LENGTH_SHORT
                        ).show()
                    } else {
                        // Cogemos el primer resultado y mostramos su detalle,
                        // igual que hace la búsqueda por ID
                        mostrarDetalleAlumno(lista.first())
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

    // ---------- Pantalla mostrar_alumno ----------

    private fun mostrarDetalleAlumno(e: Estudiante) {
        // Carga la ventana
        setContentView(R.layout.mostrar_alumno)

        // Carga la imagen desde la url que devuelve la api
        findViewById<ImageView>(R.id.detalle_imagen).load(e.imagen) {
            crossfade(true)
            placeholder(R.mipmap.ejemplo)   // mientras carga
            error(R.mipmap.ejemplo)         // si falla
            size(Size.ORIGINAL)
        }

        // Cargar datos en su campo respectivo
        findViewById<TextView>(R.id.detalle_nombre).text            = e.nombre
        findViewById<TextView>(R.id.detalle_alias).text             = e.alias
        findViewById<TextView>(R.id.detalle_casa).text              = e.casa
        findViewById<TextView>(R.id.detalle_especie).text           = e.especie
        findViewById<TextView>(R.id.detalle_genero).text            = e.genero
        findViewById<TextView>(R.id.detalle_fecha_nacimiento).text  = e.fecha_nacimiento
        findViewById<TextView>(R.id.detalle_patronus).text          = e.patronus
        findViewById<TextView>(R.id.detalle_nacionalidad).text      = e.nacionalidad
    }

    // ---------- Configuración ----------

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

    private fun configurarSpinnerIdioma() {
        // Lista de códigos de idioma. El orden debe coincidir con el de las opciones del spinner:
        // posición 0 = español, 1 = euskera, 2 = inglés
        val codigos = listOf("es", "eu", "en")

        // Obtiene el idioma que se ha elegido en la app (por ejemplo "eu").
        // Si el usuario aún no ha elegido ninguno, el texto viene vacío
        val tags = AppCompatDelegate.getApplicationLocales().toLanguageTags()

        // Si no hay idioma elegido, usa el idioma del móvil; si lo hay, se queda con
        // las 2 primeras letras ("es")
        val actual = if (tags.isEmpty()) java.util.Locale.getDefault().language else tags.take(2)

        // Busca en qué posición de la lista está ese idioma (0, 1 o 2).
        // Si no lo encuentra (devuelve -1), coerceAtLeast(0) se asegura que el valor no sea menor al minimo (0)
        val posicion = codigos.indexOf(actual).coerceAtLeast(0)

        // Deja el spinner marcado en el idioma actual.
        // El "false" indica que no hay que animar ni disparar el listener al hacerlo,
        // así evitamos que se cambie el idioma solo al abrir la pantalla
        binding.selectorIdioma.setSelection(posicion, false)

        // Define qué hacer cuando el usuario selecciona una opción del spinner
        binding.selectorIdioma.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {

            // Se ejecuta cada vez que se selecciona un elemento. "pos" es la posición elegida
            override fun onItemSelected(p: AdapterView<*>?, v: View?, pos: Int, id: Long) {
                // Solo cambia el idioma si el elegido es distinto al actual;
                // así no se recarga la pantalla sin necesidad (ni se entra en bucle)
                if (pos != posicion) {
                    // Aplica el nuevo idioma a toda la app. Android recrea la Activity
                    // y carga los textos del values-xx correspondiente
                    AppCompatDelegate.setApplicationLocales(
                        LocaleListCompat.forLanguageTags(codigos[pos])
                    )
                }
            }

            // Se ejecuta si no hay nada seleccionado. No necesitamos hacer nada aquí
            override fun onNothingSelected(p: AdapterView<*>?) {}
        }
    }
}