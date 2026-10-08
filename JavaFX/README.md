# DumbleFX

Cliente JavaFX adaptado al contrato OpenAPI de la API DumbleWindows.

## Configuración

En desarrollo se lee `src/main/resources/config.properties`. Si existe un
`config.properties` en el directorio de trabajo, tiene prioridad. Puedes elegir
un archivo externo con `-Ddumblefx.config=C:\ruta\config.properties` en las
opciones de la JVM de IntelliJ. Las variables de entorno del mismo nombre
prevalecen sobre el archivo. Reinicia la aplicación después de cambiarlo.

```properties
DUMBLEFX_API_URL=https://dumblewindows-api.tail6bccfa.ts.net
DUMBLEFX_API_URL_CONSULTA_ID=https://dumblewindows-api.tail6bccfa.ts.net/estudiantes/{student_id}
DUMBLEFX_API_URL_CONSULTA_DATOS=https://dumblewindows-api.tail6bccfa.ts.net/estudiantes
DUMBLEFX_API_URL_CREAR=https://dumblewindows-api.tail6bccfa.ts.net/matriculas
DUMBLEFX_API_URL_ELIMINAR=https://dumblewindows-api.tail6bccfa.ts.net/matriculas/{student_id}
DUMBLEFX_API_URL_ACTUALIZAR=https://dumblewindows-api.tail6bccfa.ts.net/matriculas/{student_id}
DUMBLEFX_API_KEY=TU_CLAVE
```

Las URLs de operación son completas y tienen prioridad sobre la URL base.
Conserva literalmente `{student_id}`; el cliente lo sustituye por el ID
codificado. No utilices `/docs` como URL de las operaciones.

La clave se envía mediante `X-API-Key`. No se imprime en los mensajes de error.
El archivo privado se ignora en Git y no se empaqueta en el JAR; para distribuir
la aplicación utiliza un archivo externo o variables de entorno. Una clave en
un cliente de escritorio sigue siendo extraíble: esto no es autenticación por
usuario ni protección contra la reutilización de una clave robada.

## Operaciones y formatos

| Pantalla | Método | Ruta | Datos |
|---|---|---|---|
| Consulta por ID | GET | `/estudiantes/{student_id}` | ID textual, no el ID interno de la matrícula |
| Consulta por datos | GET | `/estudiantes` | `name`, `house` opcionales; `limit` entre 1 y 100 |
| Crear estudiante | POST | `/matriculas` | JSON de matrícula |
| Actualizar estudiante | PUT | `/matriculas/{student_id}` | JSON completo de matrícula |
| Eliminar estudiante | DELETE | `/matriculas/{student_id}` | Sin cuerpo |

Las búsquedas envían los filtros a la API, no filtran una lista local.
Las respuestas de consulta usan `id`, `nombre`, `casa`, `nacimiento` o
`fecha_nacimiento`, y `origen`; los IDs pueden ser UUID o texto.

Crear y actualizar envían exclusivamente este formato:

```json
{
  "student_id": "estudiante-001",
  "full_name": "Nombre Apellidos",
  "house": "Gryffindor",
  "birth_date": "2000-01-31"
}
```

ID y nombre completo son obligatorios (máximos 64 y 200 caracteres).
Casa es opcional (máximo 80). Fecha es opcional, `AAAA-MM-DD`; los campos
opcionales vacíos se envían como `null`. PUT conserva el ID cargado.
La respuesta de matrícula incluye `student_id`, `full_name`, `house`,
`birth_date` y un `id` numérico interno, que no se usa como ID de consulta.
Eliminar devuelve un objeto de confirmación, no una ficha de estudiante.

**Límite de la API:** la búsqueda por datos consulta estudiantes históricos y
no incluye matrículas nuevas. Las escrituras afectan únicamente a las matrículas
de MariaDB; no permiten modificar ni eliminar datos históricos. Una matrícula
nueva se puede comprobar usando la consulta por su `student_id`.

## Ejecutar

Desde la raíz del proyecto: `mvnw.cmd javafx:run`, o ejecuta `Launcher` en IntelliJ.
Para compilar: `mvnw.cmd clean package` (JDK 21 o compatible).

## Estructura

- `ApiConfig`: carga del archivo y variables de entorno; URLs por operación.
- `ApiClient`: HTTP, cabeceras, codificación y errores, incluidos detalles 422.
- `Alumno`: respuesta de consulta; `Matricula`: solicitud y respuesta CRUD.
- `MainController`: navegación del menú de cinco botones.
- `StudentController`: validación y tareas HTTP en segundo plano.
- Los cinco FXML: formularios específicos del contrato real.
