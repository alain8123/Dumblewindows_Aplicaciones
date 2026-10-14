package com.dm2.dumbledroid

import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService_id {
    @GET("students") // o el endpoint que sea
    suspend fun getEstudiantesPorId(@Query("id") id: String): List<Estudiante>

    @GET(Constantes.PATH_DATOS)   // "/estudiantes"
    suspend fun getEstudiantesPorDatos(
        @Query("name") name: String,
        @Query("house") house: String,
        @Query("limit") limit: Int = 20
    ): List<Estudiante>
}