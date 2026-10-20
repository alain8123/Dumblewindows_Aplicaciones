package com.dm2.dumbledroid.Db

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper

/**
 * Helper de SQLite que crea y gestiona la base de datos local usada como caché
 * de estudiantes.
 *
 * Tabla `estudiantes`:
 *  - `id`        → clave primaria (ID que devuelve la API, ej. "GR-HAR-POT").
 *  - `json`      → el objeto Estudiante serializado a JSON (más flexible que columnas).
 *  - `timestamp` → instante en el que se guardó, para aplicar la expiración de 24h.
 */
class DbHelper(context: Context) : SQLiteOpenHelper(
    context,
    DB_NAME,
    null,
    DB_VERSION
) {

    companion object {
        /** Nombre del archivo de la base de datos. */
        const val DB_NAME = "dumbledroid_cache.db"

        /** Versión del esquema. Súbela cuando cambies la estructura de la tabla. */
        const val DB_VERSION = 1

        /** Nombre de la tabla. */
        const val TABLA_ESTUDIANTES = "estudiantes"

        // Nombres de las columnas.
        const val COL_ID        = "id"
        const val COL_JSON      = "json"
        const val COL_TIMESTAMP = "timestamp"
    }

    /**
     * Se ejecuta la primera vez que se crea la base de datos.
     * Crea la tabla con las columnas necesarias.
     */
    override fun onCreate(db: SQLiteDatabase) {
        val crearTabla = """
            CREATE TABLE $TABLA_ESTUDIANTES (
                $COL_ID TEXT PRIMARY KEY,
                $COL_JSON TEXT NOT NULL,
                $COL_TIMESTAMP INTEGER NOT NULL
            )
        """.trimIndent()
        db.execSQL(crearTabla)
    }

    /**
     * Se ejecuta al cambiar [DB_VERSION].
     * En una caché es aceptable tirar la tabla y recrearla.
     */
    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS $TABLA_ESTUDIANTES")
        onCreate(db)
    }
}