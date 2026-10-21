package com.dm2.dumbledroid.Utiles

import android.content.Context
import android.os.Handler
import android.os.Looper
import android.widget.Toast
import com.dm2.dumbledroid.Api.ApiService_id
import com.dm2.dumbledroid.Dao.EstudianteDao
import com.dm2.dumbledroid.Modelos.Estudiante
import com.dm2.dumbledroid.R
import com.google.gson.Gson
import com.google.gson.JsonElement
import com.google.gson.reflect.TypeToken

/**
 * Repositorio de estudiantes con caché SQLite.
 *
 * Estrategia "cache-first":
 *  1. Consulta la caché local (SQLite).
 *  2. Si hay resultado válido (< 24h) → lo devuelve (offline OK).
 *  3. Si no hay o ha caducado → llama a la API.
 *  4. Guarda en SQLite todo lo recibido de la API para futuras búsquedas.
 *
 * La app solo consulta el servidor: no crea, edita ni borra estudiantes remotos.
 * La caché local sí se escribe y reemplaza al recibir resultados y se limpia al caducar.
 *
 * Importante: encontrar cualquier coincidencia local evita consultar la API.
 * La caché puede ser parcial y no asegura resultados completos ni recién actualizados.
 * Offline solo funciona si la consulta encuentra datos vigentes ya guardados.
 * MainActivity llama estas búsquedas desde Dispatchers.IO porque el DAO es síncrono.
 *
 * @param context Contexto de la aplicación.
 * @param api     Servicio Retrofit ya configurado.
 * @author Kenneth Alvarez
 */
class EstudianteRepository(
    private val context: Context,
    private val api: ApiService_id
) {

    private val dao = EstudianteDao(context)

    // ==========================================================
    //                    BÚSQUEDA POR ID
    // ==========================================================

    /**
     * Busca estudiantes por ID. La API puede devolver un array u objeto, así que
     * usamos el endpoint crudo ([ApiService_id.getEstudianteRaw]) y normalizamos.
     *
     * Flujo:
     *  1. Se busca en caché con LIKE del `base` (por si hay duplicados).
     *  2. Si no hay nada, se llama a la API.
     *  3. Se guarda cada estudiante por separado en la caché.
     *
     * @param base Prefijo del ID (ej. "GR-JAM-POT", sin sufijo -02).
     * @param id   ID completo normalizado a mayúsculas por la Activity; se usa en la API.
     * @return Lista de estudiantes encontrados.
     * @author Kenneth Alvarez
     */
    suspend fun getEstudiantesPorId(base: String, id: String): List<Estudiante> {
        // 1. Caché.
        val enCache = dao.buscarPorIdBase(base)
        if (enCache.isNotEmpty()){
            info_import(context.getString(R.string.mostrado_de_cache))

            return enCache
        }

        // 2. API.
        val json = api.getEstudianteRaw(id)
        val deApi = normalizarRespuesta(json)

        // 3. Guardar en caché.
        dao.guardarTodos(deApi)

        info_import(context.getString(R.string.mostrado_de_internet))
        return deApi
    }

    // ==========================================================
    //                   BÚSQUEDA POR DATOS
    // ==========================================================

    /**
     * Busca estudiantes por nombre y/o casa.
     * La caché compara texto parcial en nombre/apellido y casa completa.
     * Si no encuentra coincidencias, envía name y house al servidor; sus reglas
     * de filtrado dependen de la API. El servicio pide un máximo de 20 resultados.
     *
     * @param nombre Texto de nombre (en caché, también apellido); puede estar vacío.
     * @param casa   Casa a buscar (puede estar vacía).
     * @return Lista de estudiantes.
     * @author Kenneth Alvarez
     */
    suspend fun getEstudiantesPorDatos(nombre: String, casa: String): List<Estudiante> {
        // 1. Caché.
        val enCache = dao.buscarPorDatos(nombre, casa)
        if (enCache.isNotEmpty()){
            info_import(context.getString(R.string.mostrado_de_cache))
            return enCache
        }

        // 2. API.
        val deApi = api.getEstudiantesPorDatos(name = nombre, house = casa)

        // 3. Guardar en caché.
        dao.guardarTodos(deApi)

        info_import(context.getString(R.string.mostrado_de_internet))
        return deApi
    }

    /**
     * Publica un aviso de procedencia de datos (caché o Internet).
     * Las consultas llegan desde IO: Handler envía el Toast al hilo principal
     * mediante su Looper, para no manipular la interfaz desde el hilo de trabajo.
     *
     * @param mensaje Texto a mostrar.
     * @author Kenneth Alvarez
     */
    fun info_import(mensaje: String) {
        Handler(Looper.getMainLooper()).post {
            Toast.makeText(context, mensaje, Toast.LENGTH_SHORT).show()
        }
    }

    // ==========================================================
    //                   ÚLTIMO CONSULTADO
    // ==========================================================

    /**
     * Marca un estudiante como "el último consultado".
     * Se llama cuando el usuario abre la pantalla de detalle.
     *
     * Guarda el ID en SharedPreferences para que el botón del menú principal
     * muestre exactamente este estudiante, sin depender del timestamp.
     *
     * @param estudiante Estudiante que el usuario acaba de abrir.
     * @author Kenneth Alvarez
     */
    fun marcarComoConsultado(estudiante: Estudiante) {
        dao.marcarUltimoConsultado(estudiante.id)
    }

    /**
     * Devuelve el último estudiante consultado (marcado explícitamente).
     * Se usa para el botón del menú principal.
     *
     * @return Último estudiante consultado o null.
     * @author Kenneth Alvarez
     */
    fun getUltimoConsultado(): Estudiante? = dao.obtenerUltimoConsultado()

    /**
     * Devuelve el último estudiante guardado en la caché (por timestamp).
     * Ya NO se usa para el botón principal, pero puede servir para depuración.
     *
     * @return Último estudiante guardado o null.
     * @author Kenneth Alvarez
     */
    fun getUltimoEstudiante(): Estudiante? = dao.obtenerUltimo()

    /**
     * Limpia las entradas caducadas (> 24h) de la caché.
     * Conviene llamarlo en `onCreate` de la Activity.
     *
     * @author Kenneth Alvarez
     */
    fun limpiarCacheCaducada() = dao.limpiarCaducados()

    // ==========================================================
    //                     UTILIDADES
    // ==========================================================

    /**
     * Normaliza la respuesta de la API a una lista de estudiantes.
     * La API puede devolver array u objeto según el caso.
     *
     * Un array se convierte con TypeToken para conservar su tipo genérico;
     * un objeto se envuelve en una lista de un elemento. Otros tipos de JSON
     * producen una lista vacía; errores de deserialización se propagan al llamador.
     *
     * @param json Respuesta cruda de Retrofit.
     * @return Lista de estudiantes (posiblemente vacía, nunca null).
     * @author Kenneth Alvarez
     */
    private fun normalizarRespuesta(json: JsonElement): List<Estudiante> {
        val gson = Gson()
        return when {
            json.isJsonArray -> {
                val tipo = object : TypeToken<List<Estudiante>>() {}.type
                gson.fromJson(json, tipo) ?: emptyList()
            }
            json.isJsonObject -> {
                val unico = gson.fromJson(json, Estudiante::class.java)
                listOfNotNull(unico)
            }
            else -> emptyList()
        }
    }
}
