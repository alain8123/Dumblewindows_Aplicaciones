package com.dm2.dumbledroid.DialogFragment

import android.app.Dialog
import android.os.Bundle
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.DialogFragment

/**
 * Diálogo de ayuda reutilizable para la app.
 *
 * Muestra un título y un mensaje en un [androidx.appcompat.app.AlertDialog]. Los datos NO se pasan
 * por constructor (eso rompe la recreación al girar), sino a través del
 * [android.os.Bundle] de argumentos, siguiendo el patrón `newInstance()` recomendado
 * por Android.
 *
 * Uso:
 * ```
 * val dialogo = MiDialogoFragment.newInstance("Título", "Mensaje")
 * dialogo.show(supportFragmentManager, "MiDialogo")
 * ```
 */
class MiDialogoFragment : DialogFragment() {

    /**
     * Crea una nueva instancia del diálogo con los textos indicados.
     * Los argumentos se guardan en el [android.os.Bundle] para que Android pueda
     * reconstruir el Fragment al recrear la Activity (giro, cambio de idioma, etc.).
     *
     * @param titulo Texto que aparecerá en el título del diálogo.
     * @param texto  Texto del cuerpo del diálogo.
     * @return Un [MiDialogoFragment] listo para mostrar con `show()`.
     */
    companion object {
        // Claves de las claves usadas dentro del Bundle de argumentos.
        private const val ARG_TITULO = "arg_titulo"
        private const val ARG_TEXTO  = "arg_texto"

        /**
         * Crea una instancia del diálogo con título y texto.
         *
         * @param titulo Texto del título.
         * @param texto  Texto del cuerpo.
         * @return Instancia lista para mostrar.
         */
        fun newInstance(titulo: String, texto: String) = MiDialogoFragment().apply {
            arguments = Bundle().apply {
                putString(ARG_TITULO, titulo)
                putString(ARG_TEXTO, texto)
            }
        }
    }

    /**
     * Construye el diálogo a partir de los argumentos guardados en el Bundle.
     * Se ejecuta tanto la primera vez como al recrear la Activity.
     */
    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        // Recuperamos los textos del Bundle (o cadena vacía si no existen).
        val titulo = arguments?.getString(ARG_TITULO).orEmpty()
        val texto  = arguments?.getString(ARG_TEXTO).orEmpty()

        return AlertDialog.Builder(requireContext())
            .setTitle(titulo)
            .setMessage(texto)
            .create()
    }
}