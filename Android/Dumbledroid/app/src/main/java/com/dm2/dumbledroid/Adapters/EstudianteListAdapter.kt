package com.dm2.dumbledroid.Adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.dm2.dumbledroid.Modelos.Estudiante
import com.dm2.dumbledroid.R

/**
 * Adaptador de la lista de estudiantes.
 *
 * @param onClick Callback ejecutado al pulsar un elemento.
 */
class EstudianteListAdapter(
 private val onClick: (Estudiante) -> Unit
) : ListAdapter<Estudiante, EstudianteListAdapter.VH>(D) {

 override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
  VH(LayoutInflater.from(parent.context).inflate(R.layout.fila_alumno, parent, false))

 override fun onBindViewHolder(holder: VH, position: Int) {
  val item = getItem(position)
  holder.bind(item)
  holder.itemView.setOnClickListener { onClick(item) }
 }

 /**
  * ViewHolder de cada fila del RecyclerView.
  * Se encarga de asignar los datos del estudiante a los TextView.
  */
 class VH(v: View) : RecyclerView.ViewHolder(v) {
  val n: TextView = v.findViewById(R.id.fila_nombre)
  val ap: TextView = v.findViewById(R.id.fila_apellido)
  val al: TextView = v.findViewById(R.id.fila_alias)
  val c: TextView = v.findViewById(R.id.fila_casa)

  /**
   * Asigna los datos del estudiante a las vistas.
   * Se usa `orEmpty()` para mostrar cadena vacía en lugar de "null".
   *
   * @param e Estudiante a mostrar.
   */
  fun bind(e: Estudiante) {
   n.text = e.nombre.orEmpty()
   ap.text = e.apellido.orEmpty()
   al.text = e.alias.orEmpty()
   c.text = e.casa.orEmpty()
  }
 }

 /**
  * Comparador para el cálculo de diferencias de la lista.
  */
 object D : DiffUtil.ItemCallback<Estudiante>() {
  override fun areItemsTheSame(a: Estudiante, b: Estudiante) = a.id == b.id
  override fun areContentsTheSame(a: Estudiante, b: Estudiante) = a == b
 }
}