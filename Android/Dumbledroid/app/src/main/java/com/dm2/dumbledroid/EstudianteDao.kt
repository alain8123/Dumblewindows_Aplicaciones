package com.dm2.dumbledroid.db

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import com.dm2.dumbledroid.Estudiante
import com.google.gson.Gson

/**
 * DAO de estudiantes para la caché SQLite.
 *
 * La app es de **solo consulta**, por lo que este DAO solo contempla:
 *  - Guardar estudiantes (al recibir datos de la API).
 *  - Buscar por ID, por datos y obtener el último consultado.
 *  - Borrar entradas caducadas automáticamente.
 */
class EstudianteDao(context: Context) {

    private val dbHelper = DbHelper(context)
    private val gson = Gson()
    private val prefs = context.getSharedPreferences("dumbledroid_prefs", Context.MODE_PRIVATE)

    private companion object {
        const val KEY_ULTIMO_ID = "ultimo_consultado_id"
    }

    /** Tiempo de vida de la caché: 24 horas en milisegundos. */
    private val TTL_MS = 24 * 60 * 60 * 1000L

    // ==========================================================
    //                       ESCRITURA
    // ==========================================================

    /**
     * Guarda o actualiza un estudiante en la caché.
     * Si ya existía (mismo ID), se reemplaza y se actualiza el timestamp.
     *
     * @param estudiante Estudiante recibido de la API.
     */
    fun guardar(estudiante: Estudiante) {
        val db = dbHelper.writableDatabase
        val valores = ContentValues().apply {
            put(DbHelper.COL_ID, estudiante.id)
            put(DbHelper.COL_JSON, gson.toJson(estudiante))
            put(DbHelper.COL_TIMESTAMP, System.currentTimeMillis())
        }
        db.insertWithOnConflict(
            DbHelper.TABLA_ESTUDIANTES,
            null,
            valores,
            SQLiteDatabase.CONFLICT_REPLACE
        )
        db.close()
    }

    /**
     * Guarda una lista completa de estudiantes (por ejemplo, resultados de una búsqueda).
     *
     * @param lista Estudiantes a guardar.
     */
    fun guardarTodos(lista: List<Estudiante>) {
        lista.forEach { guardar(it) }
    }

    // ==========================================================
    //                       LECTURA
    // ==========================================================

    /**
     * Recupera un estudiante por ID si está en caché y NO ha caducado.
     *
     * @param id Identificador exacto del estudiante.
     * @return El [Estudiante] encontrado, o `null` si no existe o caducó.
     */
    fun obtenerPorId(id: String): Estudiante? {
        val db = dbHelper.readableDatabase
        val cursor = db.query(
            DbHelper.TABLA_ESTUDIANTES,
            arrayOf(DbHelper.COL_JSON, DbHelper.COL_TIMESTAMP),
            "${DbHelper.COL_ID} = ?",
            arrayOf(id),
            null, null, null
        )

        var estudiante: Estudiante? = null
        if (cursor.moveToFirst()) {
            val json = cursor.getString(cursor.getColumnIndexOrThrow(DbHelper.COL_JSON))
            val ts = cursor.getLong(cursor.getColumnIndexOrThrow(DbHelper.COL_TIMESTAMP))

            if (System.currentTimeMillis() - ts < TTL_MS) {
                estudiante = gson.fromJson(json, Estudiante::class.java)
            } else {
                borrar(id)
            }
        }
        cursor.close()
        db.close()
        return estudiante
    }

    /**
     * Busca en caché todos los estudiantes cuyo ID EMPIECE por el prefijo dado.
     * Se usa para IDs tipo "GR-JAM-POT" que pueden tener variantes "GR-JAM-POT-02".
     *
     * @param base Prefijo del ID (ej. "GR-JAM-POT").
     * @return Lista de estudiantes vigentes que comienzan por [base].
     */
    fun buscarPorIdBase(base: String): List<Estudiante> {
        val db = dbHelper.readableDatabase
        val resultado = mutableListOf<Estudiante>()
        val cursor = db.query(
            DbHelper.TABLA_ESTUDIANTES,
            arrayOf(DbHelper.COL_JSON, DbHelper.COL_TIMESTAMP),
            "${DbHelper.COL_ID} LIKE ?",
            arrayOf("$base%"),
            null, null, null
        )

        val ahora = System.currentTimeMillis()
        while (cursor.moveToNext()) {
            val json = cursor.getString(cursor.getColumnIndexOrThrow(DbHelper.COL_JSON))
            val ts = cursor.getLong(cursor.getColumnIndexOrThrow(DbHelper.COL_TIMESTAMP))
            if (ahora - ts >= TTL_MS) continue
            resultado.add(gson.fromJson(json, Estudiante::class.java))
        }
        cursor.close()
        db.close()
        return resultado
    }

    /**
     * Devuelve el estudiante más reciente de la caché (el último consultado),
     * siempre que siga vigente (< 24h).
     *
     * @return El último [Estudiante] consultado o `null`.
     */
    fun obtenerUltimo(): Estudiante? {
        val db = dbHelper.readableDatabase
        val cursor = db.query(
            DbHelper.TABLA_ESTUDIANTES,
            arrayOf(DbHelper.COL_JSON, DbHelper.COL_TIMESTAMP),
            null, null, null, null,
            "${DbHelper.COL_TIMESTAMP} DESC",
            "1"
        )

        var estudiante: Estudiante? = null
        if (cursor.moveToFirst()) {
            val json = cursor.getString(cursor.getColumnIndexOrThrow(DbHelper.COL_JSON))
            val ts = cursor.getLong(cursor.getColumnIndexOrThrow(DbHelper.COL_TIMESTAMP))
            if (System.currentTimeMillis() - ts < TTL_MS) {
                estudiante = gson.fromJson(json, Estudiante::class.java)
            }
        }
        cursor.close()
        db.close()
        return estudiante
    }

    /**
     * Busca en caché por nombre/alias y/o casa (case-insensitive, coincidencia parcial).
     *
     * Como el modelo `Estudiante` tiene campos nullable, se usa `orEmpty()` para
     * evitar NullPointerException al llamar a métodos como `contains` o `equals`.
     *
     * @param nombre Texto a buscar en nombre/alias (puede estar vacío).
     * @param casa   Casa a buscar (puede estar vacía).
     * @return Lista de estudiantes que coinciden y siguen vigentes.
     */
    fun buscarPorDatos(nombre: String, casa: String): List<Estudiante> {
        val db = dbHelper.readableDatabase
        val resultado = mutableListOf<Estudiante>()

        val cursor = db.query(
            DbHelper.TABLA_ESTUDIANTES,
            arrayOf(DbHelper.COL_JSON, DbHelper.COL_TIMESTAMP),
            null, null, null, null, null
        )

        val ahora = System.currentTimeMillis()
        while (cursor.moveToNext()) {
            val json = cursor.getString(cursor.getColumnIndexOrThrow(DbHelper.COL_JSON))
            val ts = cursor.getLong(cursor.getColumnIndexOrThrow(DbHelper.COL_TIMESTAMP))
            if (ahora - ts >= TTL_MS) continue

            val e = gson.fromJson(json, Estudiante::class.java)

            // Usamos orEmpty() para tratar los null como cadenas vacías.
            val coincideNombre = nombre.isEmpty() ||
                    e.nombre.orEmpty().contains(nombre, ignoreCase = true) ||
                    e.apellido.orEmpty().contains(nombre, ignoreCase = true)
            val coincideCasa = casa.isEmpty() ||
                    e.casa.orEmpty().equals(casa, ignoreCase = true)

            if (coincideNombre && coincideCasa) resultado.add(e)
        }
        cursor.close()
        db.close()
        return resultado
    }

    // ==========================================================
    //                       LIMPIEZA
    // ==========================================================

    /**
     * Borra una entrada concreta (uso interno cuando caduca al leerla).
     *
     * @param id ID del estudiante a borrar.
     */
    private fun borrar(id: String) {
        val db = dbHelper.writableDatabase
        db.delete(
            DbHelper.TABLA_ESTUDIANTES,
            "${DbHelper.COL_ID} = ?",
            arrayOf(id)
        )
        db.close()
    }

    /**
     * Borra todas las entradas caducadas (> 24h).
     * Se llama al arrancar la app para dejar limpia la base.
     */
    fun limpiarCaducados() {
        val db = dbHelper.writableDatabase
        val limite = System.currentTimeMillis() - TTL_MS
        db.delete(
            DbHelper.TABLA_ESTUDIANTES,
            "${DbHelper.COL_TIMESTAMP} < ?",
            arrayOf(limite.toString())
        )
        db.close()
    }

    // ==========================================================
    //                  ÚLTIMO CONSULTADO
    // ==========================================================

    /**
     * Marca explícitamente un estudiante como "el último consultado".
     *
     * A diferencia del timestamp (que se actualiza cada vez que se guarda
     * cualquier estudiante), este método guarda el ID en SharedPreferences,
     * por lo que solo cambia cuando el usuario abre un detalle.
     *
     * @param id ID del estudiante que el usuario acaba de consultar.
     */
    fun marcarUltimoConsultado(id: String) {
        prefs.edit().putString(KEY_ULTIMO_ID, id).apply()
    }

    /**
     * Devuelve el estudiante marcado como "último consultado", o `null`
     * si no hay ninguno o si el ID guardado ya no existe en la caché.
     *
     * @return El último [Estudiante] consultado, o `null`.
     */
    fun obtenerUltimoConsultado(): Estudiante? {
        val id = prefs.getString(KEY_ULTIMO_ID, null) ?: return null
        return obtenerPorId(id)
    }
}