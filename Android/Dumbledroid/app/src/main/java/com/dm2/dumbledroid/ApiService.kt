package com.dm2.dumbledroid

import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService_id {
    @GET(Constantes.PATH_ID)
    suspend fun getEstudiantePorId(
        @Path("student_id") id: String
    ): Estudiante

    @GET(Constantes.PATH_DATOS)   // "/estudiantes"
    suspend fun getEstudiantesPorDatos(
        @Query("name") name: String,
        @Query("house") house: String,
        @Query("limit") limit: Int = 20
    ): List<Estudiante>
}