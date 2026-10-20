# Dumbledroid: guía para entender el proyecto

Esta aplicación Android consulta estudiantes en una API, muestra sus resultados
y guarda una copia temporal en el móvil. No crea, edita ni elimina estudiantes
del servidor. Esta guía explica el código existente.

## 1. Por dónde empezar

1. Abre esta carpeta (`Dumbledroid`) en Android Studio, no la carpeta superior `Android`.
2. Lee esta guía hasta el recorrido de búsqueda.
3. Abre `app/src/main/java/com/dm2/dumbledroid/MainActivity.kt`: coordina la app.
4. Sigue las llamadas a `Utiles/EstudianteRepository.kt`, `Api/ApiService.kt`
   y `Dao/EstudianteDao.kt` para entender de dónde salen los datos.
5. Mira los XML de `app/src/main/res/layout` junto a las funciones que los cargan.

Los comentarios `/** ... */` son documentación Kotlin (KDoc): explican la función,
sus entradas y su resultado. Los comentarios `//` explican pasos concretos.
En XML los comentarios se escriben como `<!-- ... -->`.

## 2. Vocabulario mínimo

| Concepto | Qué significa en esta aplicación |
| --- | --- |
| Activity | Ventana Android que controla la interfaz; aquí solo hay MainActivity. |
| Layout XML | Descripción de vistas, tamaños, posiciones y enlaces de botones. |
| View | Un elemento visible: botón, campo de entrada, imagen o contenedor. |
| ID de vista | Nombre de un control, como et_estudiante; no es el ID de un alumno. |
| Binding | Clase generada para acceder a vistas de un layout sin buscar cada ID. |
| API | Servicio remoto al que se piden estudiantes mediante HTTP. |
| JSON | Texto estructurado que el servidor envía y Gson convierte en objetos. |
| Retrofit / OkHttp | Retrofit describe las consultas; OkHttp envía las solicitudes. |
| Modelo | Objeto Kotlin que representa datos; aquí, Estudiante. |
| Repositorio | Decide si devolver datos locales o consultar la API. |
| DAO | Objeto que concentra operaciones de lectura y escritura en la base local. |
| SQLite | Base de datos almacenada dentro de la aplicación en el móvil. |
| Caché / TTL | Copia temporal de datos / tiempo durante el que se considera vigente. |
| Corrutina | Trabajo que puede suspenderse mientras espera; se usa para las consultas. |
| Callback | Función que se entrega a otro objeto para que la ejecute cuando ocurra algo. |
| RecyclerView | Lista que reutiliza las vistas de las filas al desplazarse. |

## 3. Mapa de archivos

Las rutas Kotlin siguientes parten de `app/src/main/java/com/dm2/dumbledroid/`.

| Archivo | Responsabilidad |
| --- | --- |
| MainActivity.kt | Menú, validación de formularios, navegación, resultados, idioma y estado. |
| Constantes.kt | Dirección del servidor y constantes de rutas; no guarda la clave. |
| Modelos/Estudiante.kt | Propiedades del alumno que vienen del JSON. |
| Api/ApiService.kt | Consultas GET y parámetros esperados por el servidor. |
| Utiles/EstudianteRepository.kt | Estrategia de caché, consulta remota y normalización del JSON. |
| Dao/EstudianteDao.kt | Guardar, recuperar y limpiar estudiantes locales. |
| Db/DbHelper.kt | Crear la tabla SQLite y gestionar la versión de su estructura. |
| Adapters/EstudianteListAdapter.kt | Convertir cada alumno en una fila y avisar de sus clics. |
| Utiles/ErrorUtils.kt | Convertir excepciones en mensajes entendibles y traducibles. |
| DialogFragment/MiDialogoFragment.kt | Ventana de ayuda reutilizable con título y texto. |

Otros archivos importantes:

- `app/src/main/AndroidManifest.xml`: permiso de Internet, tema y Activity de inicio.
- `app/build.gradle.kts`: configuración de compilación, bibliotecas, Binding y API key.
- `gradle/libs.versions.toml`: versiones y alias de varias bibliotecas Android.
- `app/src/test` y `app/src/androidTest`: pruebas existentes; no son pantallas de la app.

## 4. Qué pantalla corresponde a cada XML

Todos están dentro de `app/src/main/res/`.

| Layout | Uso | Función que lo abre |
| --- | --- | --- |
| layout/activity_main.xml | Menú e idioma | onCreate / mostrar_menu |
| layout/buscar_id.xml | Introducir ID en et_estudiante | mostrar_busqueda_id |
| layout/buscar_datos.xml | Introducir et_Nombre y et_Casa | mostrar_busqueda_datos |
| layout/alumno.xml | Lista de resultados: recycler_resultados | mostrarListaAlumnos |
| layout/fila_alumno.xml | Una fila: nombre, apellido, alias, casa | onCreateViewHolder del adaptador |
| layout/mostrar_alumno.xml | Ficha individual con imagen y datos | mostrarDetalleAlumno |

**Importante:** `alumno.xml` es la lista; `mostrar_alumno.xml` es el detalle;
`fila_alumno.xml` es un elemento de la lista. No son intercambiables.

La app cambia de pantalla con `setContentView`. No abre otra Activity para cada
formulario. `binding` pertenece solo a `activity_main.xml`; las demás pantallas
usan `findViewById` sobre el layout visible. Buscar un ID que no existe en esa
pantalla puede fallar.

`android:onClick="buscar_estudiante_id"` conecta un botón XML con la función
homónima de MainActivity. El parámetro `view: View` es el botón que Android entrega,
no el texto introducido: ese texto se lee desde el EditText.

`layout-w400dp-land` contiene alternativas de menú y detalle para horizontal con
un ancho disponible mínimo de 400dp. Android las selecciona automáticamente.
Al cambiar IDs o campos, revisa también estas variantes.

## 5. Recorrido completo de una búsqueda por ID

```text
Menú -> mostrar_busqueda_id -> buscar_id.xml
       -> escribir en et_estudiante -> pulsar bt_Buscar
       -> buscar_estudiante_id -> validar -> getEstudiantesPorId
       -> EstudianteRepository.getEstudiantesPorId
          -> EstudianteDao.buscarPorIdBase -> SQLite
          -> si no hay coincidencias vigentes: ApiService_id.getEstudianteRaw
             -> normalizar JSON -> guardarTodos en SQLite
       -> cero resultados: aviso
       -> un resultado: mostrarDetalleAlumno -> mostrar_alumno.xml
       -> varios: mostrarListaAlumnos -> alumno.xml -> fila_alumno.xml
                  -> pulsar fila -> mostrarDetalleAlumno
```

1. Se eliminan espacios exteriores y el ID se convierte a mayúsculas.
2. Se acepta `XX-XXX-XXX` o `XX-XXX-XXX-NN`, por ejemplo `GR-HAR-POT`.
   Validar el formato no demuestra que el alumno exista.
3. `idBase` quita un sufijo de dos cifras para buscar variantes locales.
   La llamada remota, si hace falta, conserva el ID completo normalizado.
4. La consulta se lanza con `lifecycleScope.launch(Dispatchers.IO)` para no
   bloquear la interfaz. `withContext(Dispatchers.Main)` vuelve al hilo de las vistas.
5. El servicio por ID devuelve `JsonElement`: el repositorio admite un objeto JSON
   o un array y los transforma en `List<Estudiante>`.
6. Los errores capturados pasan por `ErrorUtils` y se muestran en un Toast.

`ApiService_id.getEstudiantesPorId` es una declaración heredada NO usada por este
recorrido. Tiene un marcador de ruta `{student_id}` sin su parámetro `@Path`.
No copiarla como ejemplo ni invocarla sin revisar antes ese contrato.

## 6. Búsqueda por nombre y casa

`buscar_estudiante_datos` lee `et_Nombre` y `et_Casa`, valida caracteres y exige
al menos un campo. Después sigue el mismo reparto entre caché, API y presentación.

- En caché, el texto coincide parcialmente con **nombre o apellido**, no con alias.
- En caché, la casa debe coincidir completa, sin distinguir mayúsculas.
- Sin coincidencias locales, Retrofit envía `name`, `house` y `limit=20` a `/estudiantes`.
- Las reglas del servidor dependen de la API; no se deben deducir del filtro local.

## 7. Autenticación: dónde interviene la API key

La clave no se introduce en el formulario ni es un argumento de las funciones de
búsqueda actuales. El recorrido es:

```text
secrets.properties (local) -> app/build.gradle.kts al compilar
-> BuildConfig.API_KEY -> interceptor de OkHttp -> cabecera X-API-Key
```

El archivo local se llama **secrets.properties**, en la raíz de Dumbledroid;
la propiedad esperada es **API_KEY**. Existe `secrets.properties.example` como
referencia de configuración. No hace falta leerlo para entender el código.
Cambiar la clave requiere recompilar: no se vuelve a leer el archivo al buscar.
Si falta la propiedad, Gradle genera una cadena vacía y la autenticación falla.

## 8. Caché y último consultado: son cosas diferentes

SQLite contiene `dumbledroid_cache.db`, tabla `estudiantes`:

| Columna | Contenido |
| --- | --- |
| id | Clave primaria; identifica un alumno. |
| json | Objeto Estudiante completo convertido a texto con Gson. |
| timestamp | Momento de guardado, en milisegundos, para comprobar las 24 horas. |

Guardar el mismo ID reemplaza sus datos y actualiza su timestamp. Las lecturas
no devuelven registros de 24 horas o más; al iniciar se eliminan los que son
más antiguos que el límite. SQLite no elimina filas por sí solo al pasar el tiempo.

La estrategia es **caché primero**: si encuentra coincidencias locales, las devuelve
sin consultar Internet. Esto permite repetir ciertas consultas sin conexión,
pero la caché puede ser parcial y no garantiza todos los resultados del servidor.
Sin coincidencias guardadas vigentes, la búsqueda necesita acceso a la API.

El último detalle abierto se recuerda aparte, guardando su ID en SharedPreferences
(`dumbledroid_prefs`, clave `ultimo_consultado_id`). Abrir una ficha **no renueva**
su timestamp SQLite. Si ese registro caduca, el botón deja de poder recuperarlo.
`obtenerUltimo()` busca el último guardado por timestamp; no es lo mismo que
`obtenerUltimoConsultado()`, usado por el menú.

## 9. Cómo se dibujan los datos

`EstudianteListAdapter` recibe la lista mediante `submitList`. Su ViewHolder (`VH`)
conserva referencias a los cuatro TextView de una fila. `bind` les asigna los valores.
Los nombres actuales `n`, `ap`, `al` y `c` significan nombre, apellido, alias y casa.

El comparador `D` usa el ID para decidir si es el mismo alumno y la igualdad de
la data class para detectar cambios de contenido. El callback de clic se define
en MainActivity: el adaptador avisa, pero no decide la navegación.

En el modelo, `String?` significa que un campo puede ser nulo. `orEmpty()` convierte
un texto nulo en cadena vacía para mostrarlo. La ficha carga imágenes con Coil y
usa `R.mipmap.ejemplo` mientras espera o si falla la carga.

## 10. Estado, idioma y mensajes

- `onSaveInstanceState` guarda pantalla, resultados y textos en un Bundle temporal.
  `restaurarEstado` los recupera tras recrear la ventana; no reanuda la petición HTTP.
- La caché SQLite persiste entre aperturas; el Bundle y SharedPreferences cumplen
  propósitos distintos y no deben confundirse.
- El selector usa el orden `es`, `eu`, `en`, igual que `values/idiomas.xml`.
  Cambiar el idioma puede recrear la Activity.
- Los textos están en `values/strings.xml`, `values-eu` y `values-en`.
  Los mensajes escritos directamente en Kotlin, como el prefijo “Último:”, no se
  traducen automáticamente con esos archivos.
- `ErrorUtils` convierte fallos de red, HTTP y JSON en textos de recursos; no
  reintenta la consulta ni corrige el problema. HTTP 401/403 usa un aviso genérico.

## 11. Dónde cambiar algo sin perderse

| Quiero cambiar... | Debo revisar... |
| --- | --- |
| El diseño de una fila | fila_alumno.xml y los IDs del ViewHolder. |
| Los campos de la ficha | mostrar_alumno.xml, su variante horizontal y mostrarDetalleAlumno. |
| Un dato nuevo de la API | Modelo Estudiante, respuesta real de la API y presentación. |
| Reglas de búsqueda | Validación en MainActivity, filtros del DAO y contrato de ApiService. |
| Dirección del servidor | Constantes.kt y creación del servicio en setupRetrofit. |
| Duración de caché | TTL_MS en EstudianteDao y explicaciones de vigencia. |
| Estructura de SQLite | DbHelper y operaciones del DAO; la migración actual borra la caché. |
| Texto o traducción | Claves correspondientes en las tres carpetas de strings. |

No edites archivos de `build/`, Binding ni BuildConfig: se regeneran. No cambies
IDs XML sin actualizar Kotlin y las variantes de layout. No guardes claves reales
en comentarios ni en ejemplos compartidos.

## 12. Comprobaciones prácticas

Desde Android Studio, sincroniza el proyecto usando su configuración actual y
compila con Build > Make Project. Gradle declara minSdk 30 y compile/target 37;
comprueba que tu entorno tiene los componentes que solicita el proyecto.
No sustituyas sus versiones por los ejemplos antiguos del README de la carpeta superior.

Para revisar el funcionamiento manualmente:

1. Abre la búsqueda por ID e intenta un campo vacío y un formato incorrecto.
2. Consulta un ID válido conocido, con acceso al servidor y una clave configurada.
3. Busca por nombre/casa y abre una fila si se obtienen varios resultados.
4. Vuelve al menú y comprueba el botón del último detalle abierto.
5. Gira la pantalla y cambia de idioma; revisa restauración y variantes horizontales.
6. Tras una consulta válida, prueba a repetirla sin red mientras la caché siga vigente.

### Comportamientos existentes que conviene conocer

Esta documentación no corrige lógica ni afirma que se haya probado la API:

- `mostrarCargando` desactiva Buscar, pero el flujo actual no llama a
  `ocultarCargando` en todas las salidas exitosas sin resultados; el formulario
  puede quedarse en espera. No asumir que un comentario haya corregido esto.
- En el detalle se calcula `nombreCompleto`, pero el TextView usa otra concatenación
  directa con apellido; si este es nulo puede aparecer el texto “null”.
- La búsqueda local por prefijo puede devolver varias variantes incluso si se
  introdujo un ID con sufijo. Es el comportamiento del código actual.
- El caché no es una descarga completa ni una sincronización permanente del servidor.

Si detectas un fallo, distingue primero entre interfaz, validación, conexión/API,
deserialización y caché siguiendo el recorrido del apartado 5.
