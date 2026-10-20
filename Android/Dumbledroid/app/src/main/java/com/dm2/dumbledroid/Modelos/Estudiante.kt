package com.dm2.dumbledroid.Modelos

/**
 * Modelo: representa un alumno recibido como JSON de la API o recuperado de SQLite.
 * Los nombres de las propiedades coinciden con las claves JSON que usa Gson.
 * data class genera comparación de contenido (equals), que utiliza DiffUtil.
 * El signo ? permite null: un dato ausente no es lo mismo que una cadena vacía.
 * En la interfaz se usa orEmpty() para muchos campos opcionales.
 *
 * @property id Identificador usado por la API, la clave primaria local y DiffUtil.
 * @property nombre Nombre del estudiante, declarado obligatorio en este modelo.
 * @property apellido Apellido opcional; se muestra en la lista y en el detalle.
 * @property casa Casa del estudiante; interviene en las búsquedas.
 * @property alias Sobrenombre mostrado en lista y ficha; no es filtro de la caché actual.
 * @property especie Especie mostrada en la ficha.
 * @property genero Género mostrado en la ficha.
 * @property nacimiento Dato de nacimiento conservado; la ficha usa fecha_nacimiento.
 * @property fecha_nacimiento Fecha mostrada como texto, sin conversión a una fecha Kotlin.
 * @property nacionalidad Nacionalidad mostrada en la ficha.
 * @property patronus Patronus mostrado en la ficha.
 * @property patronus_origen Dato adicional conservado, no mostrado en el detalle actual.
 * @property imagen URL que Coil carga; si falla se muestra la imagen de ejemplo.
 * @property wiki Enlace conservado, sin botón de apertura implementado en el detalle.
 * @property origen Origen del registro según la API; no indica si procede de caché.
 */
data class Estudiante(
    val id : String,
    val nombre : String,
    val apellido : String?,
    val casa : String?,
    val alias : String?,
    val especie : String?,
    val genero : String?,
    val nacimiento : String?,
    val fecha_nacimiento : String?,
    val nacionalidad : String?,
    val patronus : String?,
    val patronus_origen : String?,
    val imagen : String?,
    val wiki : String?,
    val origen : String
)
