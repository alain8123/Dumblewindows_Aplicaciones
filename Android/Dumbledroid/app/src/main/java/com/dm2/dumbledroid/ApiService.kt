package com.dm2.dumbledroid

import com.google.gson.JsonElement
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService_id {
    @GET(Constantes.PATH_ID)
    suspend fun getEstudiantesPorId(
        @Query("id") id: String
    ): List<Estudiante>

    @GET("estudiantes/{student_id}")
    suspend fun getEstudianteRaw(
        @Path("student_id") studentId: String
    ): JsonElement

    @GET(Constantes.PATH_DATOS)   // "/estudiantes"
    suspend fun getEstudiantesPorDatos(
        @Query("name") name: String,
        @Query("house") house: String,
        @Query("limit") limit: Int = 20
    ): List<Estudiante>
}