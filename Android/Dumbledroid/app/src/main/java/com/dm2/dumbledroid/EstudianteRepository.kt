package com.dm2.dumbledroid.repository

import android.content.Context
import com.dm2.dumbledroid.ApiService_id
import com.dm2.dumbledroid.Estudiante
import com.dm2.dumbledroid.db.EstudianteDao
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
 * La app es de SOLO CONSULTA: nunca se modifica ni borra la caché salvo por caducidad.
 */
class EstudianteRepository(
    context: Context,
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
     * @param id   ID completo tal cual lo escribió el usuario (fallback).
     * @return Lista de estudiantes encontrados.
     */
    suspend fun getEstudiantesPorId(base: String, id: String): List<Estudiante> {
        // 1. Caché.
        val enCache = dao.buscarPorIdBase(base)
        if (enCache.isNotEmpty()) return enCache

        // 2. API.
        val json = api.getEstudianteRaw(id)
        val deApi = normalizarRespuesta(json)

        // 3. Guardar en caché.
        dao.guardarTodos(deApi)

        return deApi
    }

    // ==========================================================
    //                   BÚSQUEDA POR DATOS
    // ==========================================================

    /**
     * Busca estudiantes por nombre y/o casa.
     *
     * @param nombre Nombre o alias a buscar (puede estar vacío).
     * @param casa   Casa a buscar (puede estar vacía).
     * @return Lista de estudiantes.
     */
    suspend fun getEstudiantesPorDatos(nombre: String, casa: String): List<Estudiante> {
        // 1. Caché.
        val enCache = dao.buscarPorDatos(nombre, casa)
        if (enCache.isNotEmpty()) return enCache

        // 2. API.
        val deApi = api.getEstudiantesPorDatos(name = nombre, house = casa)

        // 3. Guardar en caché.
        dao.guardarTodos(deApi)

        return deApi
    }

    // ==========================================================
    //                   ÚLTIMO CONSULTADO
    // ==========================================================

    /**
     * Marca un estudiante como "el último consultado".
     *
     * Se llama cuando el usuario abre la pantalla de detalle de un estudiante,
     * sin importar de dónde venga (búsqueda por ID, por datos, botón "último
     * consultado", etc.).
     *
     * Al guardarlo de nuevo, se actualiza su timestamp y pasa a ser el más
     * reciente en la caché, por lo que el botón "último consultado" del menú
     * mostrará este estudiante.
     *
     * @param estudiante Estudiante que el usuario acaba de abrir.
     */
    // ✅ NUEVO MÉTODO
    fun marcarComoConsultado(estudiante: Estudiante) {
        dao.guardar(estudiante)
    }

    /**
     * Devuelve el último estudiante consultado (que aún no haya caducado).
     * Se usa para el botón del menú principal.
     */
    fun getUltimoEstudiante(): Estudiante? = dao.obtenerUltimo()

    /**
     * Limpia las entradas caducadas (> 24h) de la caché.
     * Conviene llamarlo en `onCreate` de la Activity.
     */
    fun limpiarCacheCaducada() = dao.limpiarCaducados()

    // ==========================================================
    //                     UTILIDADES
    // ==========================================================

    /**
     * Normaliza la respuesta de la API a una lista de estudiantes.
     * La API puede devolver array u objeto según el caso.
     *
     * @param json Respuesta cruda de Retrofit.
     * @return Lista de estudiantes (posiblemente vacía, nunca null).
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