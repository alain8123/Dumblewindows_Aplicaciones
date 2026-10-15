package com.dm2.dumbledroid

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView

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

 class VH(v: View) : RecyclerView.ViewHolder(v) {
  val n: TextView = v.findViewById(R.id.fila_nombre)
  val ap: TextView = v.findViewById(R.id.fila_apellido)
  val al: TextView = v.findViewById(R.id.fila_alias)
  val c: TextView = v.findViewById(R.id.fila_casa)

  fun bind(e: Estudiante) {
   n.text = e.nombre
   ap.text = e.apellido
   al.text = e.alias
   c.text = e.casa
  }
 }

 object D : DiffUtil.ItemCallback<Estudiante>() {
  override fun areItemsTheSame(a: Estudiante, b: Estudiante) = a.id == b.id
  override fun areContentsTheSame(a: Estudiante, b: Estudiante) = a == b
 }
}