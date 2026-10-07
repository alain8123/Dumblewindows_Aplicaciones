package com.dm2.dumbledroid

import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService_id {
    @GET(Constantes.PATH_ID)
    suspend fun getEstudiantePorId(
        @Path("student_id") id: String,
        @Query("api_key") apiKey: String
    ): Estudiante
}