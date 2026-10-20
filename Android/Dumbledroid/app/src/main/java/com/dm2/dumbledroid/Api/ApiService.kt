package com.dm2.dumbledroid.Api

import com.dm2.dumbledroid.Constantes
import com.dm2.dumbledroid.Modelos.Estudiante
import com.google.gson.JsonElement
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

/**
 * Contrato HTTP: Retrofit implementa esta interfaz; no hay que escribir las peticiones a mano.
 * GET indica una consulta, Path sustituye un tramo de la ruta y Query añade ?clave=valor.
 * suspend permite esperar la respuesta desde una corrutina sin bloquearla activamente.
 * La autenticación X-API-Key se añade en MainActivity.setupRetrofit, no en estos parámetros.
 * El repositorio usa getEstudianteRaw para ID y getEstudiantesPorDatos para los filtros.
 */
interface ApiService_id {
    /**
     * Declaración heredada que el flujo actual NO utiliza. PATH_ID contiene
     * {student_id}, pero aquí solo hay @Query(id), no @Path(student_id).
     * No usarla como ejemplo de búsqueda por ID; revisar ese contrato antes de invocarla.
     */
    @GET(Constantes.PATH_ID)
    suspend fun getEstudiantesPorId(
        @Query("id") id: String
    ): List<Estudiante>

    /**
     * Consulta un ID en la ruta (por ejemplo estudiantes/GR-HAR-POT).
     * Devuelve JSON sin convertir porque puede ser un objeto o un array.
     * EstudianteRepository.normalizarRespuesta unifica ambos en List<Estudiante>.
     * @param studentId ID completo ya validado por MainActivity.
     */
    @GET("estudiantes/{student_id}")
    suspend fun getEstudianteRaw(
        @Path("student_id") studentId: String
    ): JsonElement

    /**
     * Consulta /estudiantes?name=...&house=...&limit=20. Retrofit codifica los valores.
     * Los nombres name y house son los que espera el servidor, no los de los EditText.
     * @param name Texto del nombre; puede estar vacío si se indica casa.
     * @param house Casa; puede estar vacía si se indica nombre.
     * @param limit Máximo solicitado por llamada; no hay paginación implementada aquí.
     */
    @GET(Constantes.PATH_DATOS)
    suspend fun getEstudiantesPorDatos(
        @Query("name") name: String,
        @Query("house") house: String,
        @Query("limit") limit: Int = 20
    ): List<Estudiante>
}
