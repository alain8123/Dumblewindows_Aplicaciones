package com.dm2.dumbledroid

import retrofit2.http.GET
import retrofit2.http.Path

interface ApiService_id {
    @GET(Constantes.PATH_ID)
    suspend fun getEstudiantePorId(
        @Path("student_id") id: String
    ): Estudiante
}