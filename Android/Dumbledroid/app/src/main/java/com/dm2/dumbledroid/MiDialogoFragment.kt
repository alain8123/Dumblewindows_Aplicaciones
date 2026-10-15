package com.dm2.dumbledroid

import android.app.Dialog
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.DialogFragment

class MiDialogoFragment(val titulo: String, val texto: String) : DialogFragment() {

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        return AlertDialog.Builder(requireContext())
            .setTitle(titulo)
            .setMessage(texto)
            .show()   // <-- Esto es lo que faltaba
    }
}