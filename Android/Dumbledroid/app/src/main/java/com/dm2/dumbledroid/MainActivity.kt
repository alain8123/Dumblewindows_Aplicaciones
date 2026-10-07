package com.dm2.dumbledroid

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import com.dm2.dumbledroid.databinding.ActivityMainBinding
import android.widget.AdapterView
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        //setContentView(R.layout.activity_main)
        binding = ActivityMainBinding.inflate(layoutInflater)
        val view = binding.root
        setContentView(view)

        // Configurar idioma
        configurarSpinnerIdioma()
    }

    /*
     * Configura el spinner (selector) de idioma.
     * - Muestra en el spinner el idioma que está activo en la app.
     * - Cuando el usuario elige otro idioma, lo aplica y la pantalla se recarga traducida.
     */
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

    fun mostrar_busqueda_id(view : View){
        setContentView(R.layout.buscar_id)
    }

    fun mostrar_busqueda_datos(view : View){
        setContentView(R.layout.buscar_datos)
    }


    fun buscar_estudiante_id(view : View){
        Toast.makeText(this, "Buscando", Toast.LENGTH_SHORT).show()
    }

    fun buscar_estudiante_datos(view : View){
        Toast.makeText(this, "Buscando", Toast.LENGTH_SHORT).show()
    }

    fun mostrar_menu(view : View){
        setContentView(binding.root)
    }
}