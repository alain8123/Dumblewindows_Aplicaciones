package com.dm2.dumbledroid
import android.view.*;import android.widget.TextView
import androidx.recyclerview.widget.*
class EstudianteListAdapter:ListAdapter<Estudiante,EstudianteListAdapter.VH>(D){
 override fun onCreateViewHolder(p:ViewGroup,t:Int)=VH(LayoutInflater.from(p.context).inflate(R.layout.alumno,p,false))
 override fun onBindViewHolder(h:VH,i:Int)=h.bind(getItem(i))
 class VH(v:View):RecyclerView.ViewHolder(v){val n:TextView=v.findViewById(R.id.estudiante_nombre);val a:TextView=v.findViewById(R.id.estudiante_alias);val c:TextView=v.findViewById(R.id.estudiante_casa);fun bind(e:Estudiante){n.text=e.nombre;a.text=e.alias;c.text=e.casa}}
 object D:DiffUtil.ItemCallback<Estudiante>(){override fun areItemsTheSame(a:Estudiante,b:Estudiante)=a.id==b.id;override fun areContentsTheSame(a:Estudiante,b:Estudiante)=a==b}}