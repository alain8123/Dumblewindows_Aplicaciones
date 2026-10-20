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
 * Puente entre List<Estudiante> y las filas de recycler_resultados en alumno.xml.
 * ListAdapter recibe datos con submitList y recicla vistas al desplazarse.
 * El adaptador NO consulta la API ni decide la navegación: avisa mediante onClick.
 * Los nombres cortos existentes son: VH = ViewHolder, D = comparador de diferencias.
 *
 * @param onClick Callback ejecutado al pulsar un elemento.
 * @author Kenneth Alvarez
 */
class EstudianteListAdapter(
 private val onClick: (Estudiante) -> Unit
) : ListAdapter<Estudiante, EstudianteListAdapter.VH>(D) {

 /**
  * Crea una fila solo cuando RecyclerView necesita una vista nueva.
  * inflate convierte fila_alumno.xml en vistas; false deja que RecyclerView
  * las añada a su contenedor, en lugar de adjuntarlas inmediatamente.
  *
  * @param parent   Contenedor padre de la nueva vista.
  * @param viewType Tipo de vista (no usado, una sola fila).
  * @return ViewHolder con la fila inflada.
  * @author Kenneth Alvarez
  */
 override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
  VH(LayoutInflater.from(parent.context).inflate(R.layout.fila_alumno, parent, false))

 /**
  * Rellena una fila nueva o reciclada con el estudiante de esta posición.
  * El clic captura ese estudiante y lo entrega a MainActivity; no su posición,
  * que podría variar al actualizar la lista.
  *
  * @param holder   ViewHolder de la fila.
  * @param position Posición del elemento en la lista.
  * @author Kenneth Alvarez
  */
 override fun onBindViewHolder(holder: VH, position: Int) {
  val item = getItem(position)
  holder.bind(item)
  holder.itemView.setOnClickListener { onClick(item) }
 }

 /**
  * ViewHolder de cada fila del RecyclerView.
  * Se encarga de asignar los datos del estudiante a los TextView.
  *
  * @param v Vista raíz de la fila.
  * @author Kenneth Alvarez
  */
 class VH(v: View) : RecyclerView.ViewHolder(v) {
  // n = nombre; ap = apellido; al = alias; c = casa. Son las vistas de fila_alumno.xml.
  val n: TextView = v.findViewById(R.id.fila_nombre)
  val ap: TextView = v.findViewById(R.id.fila_apellido)
  val al: TextView = v.findViewById(R.id.fila_alias)
  val c: TextView = v.findViewById(R.id.fila_casa)

  /**
   * Asigna los datos del estudiante a las vistas.
   * Se usa `orEmpty()` para mostrar cadena vacía en lugar de "null".
   *
   * @param e Estudiante a mostrar.
   * @author Kenneth Alvarez
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
  *
  * @author Kenneth Alvarez
  */
 object D : DiffUtil.ItemCallback<Estudiante>() {
  /** Mismo ID: representa al mismo alumno, aunque algún dato haya cambiado. */
  override fun areItemsTheSame(a: Estudiante, b: Estudiante) = a.id == b.id
  /** Igualdad de la data class: si cambia un campo, hay que volver a dibujar la fila. */
  override fun areContentsTheSame(a: Estudiante, b: Estudiante) = a == b
 }
}
