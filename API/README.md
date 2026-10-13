# API de estudiantes de Hogwarts

## Estado del despliegue

El proyecto se ejecuta en Docker Desktop en `dm2_casa` (`C:\Users\alain\IA\api-estudiantes`). La API FastAPI publica solo el puerto `8000`; las bases no publican puertos al exterior.

Servicios:

- `api`: FastAPI, documentación automática en `/docs`, `/redoc` y `/openapi.json`.
- `mariadb`: matrículas nuevas de Secretaría (`enrollments`), único origen que admite alta, modificación y baja.
- `derby`: Gryffindor, solo lectura.
- `hsqldb`: Slytherin, solo lectura.
- `h2`: Hufflepuff, solo lectura.
- `oracle`: Ravenclaw, solo lectura.
- SQLite: archivo `/data/cache.sqlite3`, volumen `api_cache`, usado como caché temporal y separado de MariaDB.

Los cuatro CSV adjuntos se cargan en su propio motor durante la creación inicial de los contenedores. No se copian a MariaDB: la tabla `enrollments` permanece independiente.

## Mapeo y conectores

| Casa | Motor | Conector en Python | Servicio Docker |
|---|---|---|---|
| Gryffindor | Apache Derby | JayDeBeApi + JDBC | `derby:1527` |
| Slytherin | HSQLDB | JayDeBeApi + JDBC | `hsqldb:9001` |
| Hufflepuff | H2 | JayDeBeApi + JDBC | `h2:9092` |
| Ravenclaw | Oracle Free | `oracledb` | `oracle:1521/FREEPDB1` |

JayDeBeApi carga los JAR de `/opt/jdbc`. Oracle utiliza el controlador Thin de `oracledb`, sin instalar un cliente Oracle dentro de la API.

## Flujo de una consulta

1. `GET /estudiantes/{student_id}` busca primero una matrícula nueva en MariaDB.
2. Si no existe, busca una respuesta vigente en SQLite.
3. Si tampoco existe, consulta las cuatro fuentes históricas, sin escribir en ellas.
4. La respuesta encontrada se guarda en SQLite con fecha de caché; nunca se inserta en MariaDB.
5. Las consultas repetidas sirven la respuesta desde SQLite mientras no caduque (24 horas).

`GET /estudiantes?name=...&house=...&limit=...` permite búsquedas por nombre y casa a partir de los CSV originales; este listado no consulta las tablas históricas en vivo ni incluye nuevas matrículas de MariaDB. La consulta por identificador sí consulta las bases históricas cuando hay fallo de caché. Las fechas exactas se muestran como `dd/mm/yyyy`; las fechas aproximadas conservan su texto original para no inventar un día.

## Operaciones de Secretaría

- `POST /matriculas`: alta solo en MariaDB.
- `PUT /matriculas/{student_id}`: modificación solo en MariaDB.
- `DELETE /matriculas/{student_id}`: baja solo en MariaDB.

Las cuatro bases históricas no tienen endpoints de escritura.

## Arranque y comprobación

```powershell
cd C:\Users\alain\IA\api-estudiantes
docker compose config -q
docker compose up -d --build
docker compose ps
curl http://127.0.0.1:8000/health
start http://127.0.0.1:8000/docs
```

La respuesta de salud incluye `"code":"R01"`, el mapeo de las fuentes y `"no_migration":true`.

## Reinicio y persistencia

```powershell
docker compose restart
docker compose ps
```

Los volúmenes `mariadb_data`, `api_cache`, `derby_data`, `hsqldb_data`, `h2_data` y `oracle_data` sobreviven a reinicios. No borrar los volúmenes históricos salvo que se quiera volver a cargar los CSV desde cero.

## Publicación para las aplicaciones

Durante el desarrollo en la misma red, JavaFX puede usar `http://IP_DEL_PC:8000`. Para Internet se necesita un túnel HTTPS o un servidor externo; este Compose no publica MariaDB ni las cuatro fuentes históricas. Las rutas de consulta y CRUD requieren X-API-Key; utilizar solo datos ficticios y cerrar el túnel al terminar.


## Rutas en español y documentación automática

Abrir `http://127.0.0.1:8000/docs` desde el propio PC, o `http://IP_DEL_PC:8000/docs` desde un equipo que pueda llegar al servidor. Swagger UI se genera a partir de las definiciones y descripciones de `app/main.py`; también están `/redoc` y `/openapi.json`. Una ruta publicada en Swagger no implica acceso desde Internet: eso requiere configurar por separado la red o un túnel HTTPS.

| Método | Ruta nueva | Función |
|---|---|---|
| GET | `/salud` | Estado y código `R01` |
| GET | `/estudiantes?name=...&house=...&limit=20` | Listado histórico desde CSV y caché SQLite |
| GET | `/estudiantes/{student_id}` | MariaDB → SQLite → las cuatro bases históricas |
| POST | `/matriculas` | Crear nueva matrícula en MariaDB |
| PUT | `/matriculas/{student_id}` | Modificar matrícula de MariaDB |
| DELETE | `/matriculas/{student_id}` | Eliminar matrícula de MariaDB |

Los parámetros de búsqueda `name`, `house`, `limit` y los campos JSON de matrículas (`student_id`, `full_name`, `house`, `birth_date`) mantienen sus nombres actuales para no romper aplicaciones existentes; se han traducido las **rutas** y su explicación, no el contrato de datos. Para crear o modificar, enviar JSON con `student_id` y `full_name` obligatorios. Pydantic interpreta `birth_date` como `YYYY-MM-DD`; `dd/mm/yyyy` se aplica a fechas exactas del estudiante histórico devueltas en `nacimiento` y `fecha_nacimiento`.

Las rutas antiguas `/health`, `/students` y `/enrollments` siguen funcionando para clientes existentes, pero están ocultas del esquema OpenAPI; los clientes nuevos deben usar las rutas en español. Para aplicar cambios de documentación o código: `docker compose up -d --build api`. No borrar los volúmenes para reconstruir la API.

## Acceso remoto de desarrollo

URL actual: https://dumblewindows-api.tail6bccfa.ts.net/docs (probada el 04/10/2026). Desde Internet `/salud` y `/docs` devuelven 200; `/estudiantes` sin clave devuelve 401. Una consulta local con clave devolvió 200.

El contenedor `api-estudiantes-tailscale` crea un túnel HTTPS saliente hacia `http://api:8000`; las bases permanecen dentro de Docker. No se abrió ningún puerto del router. Ver el contenedor con `docker ps --filter name=api-estudiantes-tailscale`; ver la URL vigente con `docker logs api-estudiantes-tailscale`. Detener el acceso público con `docker stop api-estudiantes-tailscale` y reanudar con `docker start api-estudiantes-tailscale`.

Swagger `/docs` incluye **Authorize**. La clave se guarda solo en `.api-secret.env` configurar JavaFX con la URL base y enviar la cabecera `X-API-Key` en consultas y CRUD. No compartir la clave por chat. Una clave incrustada en una app distribuida es extraíble: para datos reales se necesitan usuarios y autorización. El PC y Docker Desktop deben mantenerse encendidos. El puerto local 8000 sigue accesible en la red del PC.

## Contenido de los .csv
El contenido de los .csv son datos de prueba.