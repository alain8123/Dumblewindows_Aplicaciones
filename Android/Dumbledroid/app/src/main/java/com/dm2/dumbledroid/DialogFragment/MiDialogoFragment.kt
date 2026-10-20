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
 *
 * @author Kenneth Alvarez
 */
class MiDialogoFragment : DialogFragment() {

    /**
     * Fábrica de diálogos y claves de los argumentos que Android puede restaurar.
     *
     * @author Kenneth Alvarez
     */
    companion object {
        // Claves compartidas entre newInstance (escritura) y onCreateDialog (lectura).
        private const val ARG_TITULO = "arg_titulo"
        private const val ARG_TEXTO  = "arg_texto"

        /**
         * Crea una instancia del diálogo con título y texto.
         *
         * @param titulo Texto del título.
         * @param texto  Texto del cuerpo.
         * @return Instancia lista para mostrar.
         * @author Kenneth Alvarez
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
     *
     * @param savedInstanceState Estado previo (no usado; los datos vienen de arguments).
     * @return Diálogo configurado con título y mensaje.
     * @author Kenneth Alvarez
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
