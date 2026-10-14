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

Al crear una matrícula, nombre y apellidos se introducen por separado. La casa
aparece como `Central` y, si se deja vacía, también se usa `Central`.
El ID es de solo lectura y se muestra mientras se rellenan los datos:
`CC-NNN-AAA` (dos letras de la casa, tres del nombre y tres del primer apellido),
en mayúsculas y sin tildes. Ejemplo: Central, Harry, Potter Evans → `CE-HAR-POT`.
La vista previa no consulta ni reserva IDs. Al guardar, la API comprueba
los duplicados y devuelve el ID definitivo, con `-02`, `-03`, etc. si procede.
El formulario muestra ese ID final y no envía el ID calculado en la petición POST.

Ejemplo de alta:

```json
{
  "full_name": "Harry",
  "apellidos": "Potter Evans",
  "house": "Central",
  "birth_date": "2000-01-31"
}
```

Nombre y apellidos son obligatorios (máximo 200 caracteres cada uno).
Casa admite hasta 80 caracteres. La fecha es opcional, en formato `AAAA-MM-DD`;
si está vacía, se omite del JSON. Las abreviaturas con menos letras conservan
las disponibles; el primer apellido sigue las mismas reglas de partículas
que la API (`de`, `del`, `van`, etc.).

En la actualización se conservan y envían los apellidos obtenidos al cargar
la matrícula, junto con el nombre, la casa, la fecha y el ID de consulta.
La API puede generar otro ID si cambia la identidad; se muestra el ID devuelto.
La respuesta de matrícula incluye `student_id`, `full_name`, `apellidos`,
`house`, `birth_date` y un `id` numérico interno, que no se usa como ID de consulta.
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
