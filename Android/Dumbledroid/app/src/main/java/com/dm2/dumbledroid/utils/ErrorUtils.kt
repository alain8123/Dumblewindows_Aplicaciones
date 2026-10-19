package com.dm2.dumbledroid.utils

import android.content.Context
import com.dm2.dumbledroid.R
import com.google.gson.JsonParseException
import retrofit2.HttpException
import java.io.IOException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

/**
 * Utilidades para traducir excepciones técnicas a mensajes amigables
 * para el usuario final.
 *
 * Mapea las excepciones más comunes (red, HTTP, parseo, etc.) a strings
 * definidos en `strings.xml`, de forma que el usuario vea algo como
 * "No hay conexión a Internet" en lugar de
 * "Unable to resolve host ...".
 */
object ErrorUtils {

    /**
     * Convierte una excepción en un mensaje amigable para el usuario.
     *
     * @param context Contexto para acceder a los recursos de strings.
     * @param e       Excepción capturada.
     * @return Mensaje amigable listo para mostrar en un Toast o AlertDialog.
     */
    fun mensajeAmigable(context: Context, e: Throwable): String {
        return when (e) {

            // Red
            is UnknownHostException ->
                context.getString(R.string.error_sin_conexion)

            is SocketTimeoutException ->
                context.getString(R.string.error_timeout)

            is IOException ->
                // Cualquier otro fallo de red (conexión interrumpida, etc.)
                context.getString(R.string.error_sin_conexion)

            // HTTP (Retrofit lanza HttpException)
            is HttpException -> when (e.code()) {
                404 -> context.getString(R.string.error_no_encontrado)
                401, 403 -> context.getString(R.string.error_servidor)
                in 500..599 -> context.getString(R.string.error_servidor)
                else -> context.getString(R.string.error_servidor)
            }

            // Parseo JSON
            is JsonParseException ->
                context.getString(R.string.error_datos)

            // Fallback
            else ->
                context.getString(R.string.error_desconocido)
        }
    }
}