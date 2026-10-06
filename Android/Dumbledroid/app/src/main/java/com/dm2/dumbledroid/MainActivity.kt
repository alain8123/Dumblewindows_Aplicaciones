package com.dm2.dumbledroid

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import com.dm2.dumbledroid.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        //setContentView(R.layout.activity_main)
        binding = ActivityMainBinding.inflate(layoutInflater)
        val view = binding.root
        setContentView(view)

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
        setContentView(R.layout.activity_main)
    }
}