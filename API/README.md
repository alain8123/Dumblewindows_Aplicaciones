# API de estudiantes — entrega portable

## Qué hace

Servicio HTTP desarrollado con FastAPI para consultar estudiantes históricos y gestionar matrículas nuevas. Se ejecuta con Docker Compose e incluye una API, MariaDB para matrículas y cuatro motores históricos:

- **MariaDB** guarda las matrículas y los datos de prueba de `data/Central.csv`; permite crearlas, modificarlas y eliminarlas.
- **Derby, HSQLDB, H2 y Oracle** alojan los registros históricos de las cuatro casas. La API los consulta, sin ofrecer operaciones de escritura sobre ellos.
- **SQLite** conserva temporalmente resultados históricos para acelerar consultas repetidas. No sustituye a las bases de datos.

La consulta de estudiantes combina MariaDB con los datos históricos y evita duplicados. Las búsquedas por identificador admiten IDs base con variantes. Los datos CSV se usan para inicializar las tablas históricas vacías y las matrículas de Central en MariaDB.

## Cómo funciona

FastAPI expone las rutas y la documentación interactiva. Las consultas y operaciones de matrícula (excepto el estado y la documentación) requieren la cabecera `X-API-Key`. La clave se configura por instalación y no debe compartirse.

La ruta `/salud` comprueba la API y MariaDB. `/docs` ofrece Swagger UI; también están disponibles `/redoc` y `/openapi.json`. Las conexiones de bases de datos son internas a la red de Docker. SQLite se guarda en un volumen persistente.

La inicialización puede crear tablas y cargar los CSV cuando las tablas están vacías. Por ello, el primer arranque escribe los datos iniciales en los motores históricos y carga `data/Central.csv` en `enrollments` de MariaDB si esa tabla está vacía. Si ya contiene alguna matrícula, no se importa Central: no se sobrescriben ni se mezclan los datos existentes. La carga conserva los IDs del CSV, valida nombres, apellidos, fechas y casa Central, y se confirma en una sola transacción; si hay un error, se revierte la carga completa. Reiniciar con una tabla no vacía no duplica ni restaura filas eliminadas. Si se vacía por completo, el siguiente arranque vuelve a cargarla. Los cambios hechos a matrículas se guardan en MariaDB. Los volúmenes Docker conservan datos entre reinicios.

## Por qué está organizado así

Separar matrículas nuevas de los registros históricos evita modificar las fuentes históricas. La caché SQLite reduce consultas repetidas sin convertirse en la fuente de verdad. Docker Compose reúne la API y sus dependencias, de modo que otro equipo pueda iniciar el conjunto sin instalar localmente Java, JDBC ni los cinco servidores de base de datos. La clave y las contraseñas se generan en el equipo destino, en lugar de trasladar credenciales del PC original.

## Contenido

- `app/main.py`: aplicación, endpoints, autenticación, consultas y caché.
- `compose.yaml`: API y servicios de base de datos aislados en Docker.
- `Dockerfile`, `db-image/`: imágenes de la API y de los motores JDBC.
- `init/mariadb.sql`: esquema inicial para matrículas nuevas.
- `data/*.csv`: datos de inicialización de ejemplo para las fuentes históricas y MariaDB (`Central.csv`).
- `requirements.txt`: dependencias Python.
- `.env.example`, `preparar_config.py`: configuración inicial sin secretos reales.

El paquete no incluye contraseñas del servidor original, bases de datos en ejecución, cachés, entornos virtuales ni respaldos de matrículas. Los CSV pueden contener datos personales o de prueba; distribuye la carpeta solo con autorización para compartirlos.

## Instalación en otro equipo

Requisitos: Docker Desktop con contenedores Linux, Python 3 para generar la configuración, conexión a Internet para descargar imágenes y paquetes, y recursos suficientes para ejecutar cinco bases de datos. El paquete está orientado a x86-64; Oracle puede requerir una plataforma compatible.

1. Copia la carpeta completa a otro equipo y abre una terminal en ella.
2. Genera credenciales propias. El script no muestra secretos ni reemplaza un `.env` existente:

   ```powershell
   python preparar_config.py
   ```

   Si el comando no está disponible, prueba `py preparar_config.py`. No compartas ni subas `.env`.

3. Valida y arranca el proyecto:

   ```powershell
   docker compose -p API config -q
   docker compose -p API up -d --build
   docker compose -p API ps
   ```

   El primer arranque descarga imágenes, inicia cinco motores y carga los CSV en las tablas históricas vacías y `Central.csv` en MariaDB si no hay matrículas. Puede tardar varios minutos.

4. Comprueba el servicio y abre la documentación:

   ```powershell
   Invoke-RestMethod http://(ip del equipo donde esta alojada la api):8000/salud
   Start-Process http://(ip del equipo donde esta alojada la api):8000/docs
   ```

   En Swagger, pulsa **Authorize** e introduce el valor local de `API_ACCESS_KEY` de `.env`.

## Endpoints principales

| Método | Ruta | Función |
|---|---|---|
| GET | `/salud` | Estado del servicio |
| GET | `/estudiantes?name=Ana&house=Central&limit=20` | Búsqueda por nombre/casa |
| GET | `/estudiantes/{student_id}` | Detalle o coincidencias por ID |
| POST | `/matriculas` | Alta en MariaDB |
| PUT | `/matriculas/{student_id}` | Modificación en MariaDB |
| DELETE | `/matriculas/{student_id}` | Baja en MariaDB |

Consulta `/docs` para los campos exactos, parámetros y respuestas actuales. La API también conserva alias en inglés para clientes anteriores.

## Mantenimiento y límites

```powershell
docker compose -p API logs --tail=100 api
docker compose -p API stop
docker compose -p API start
docker compose -p API down
```

`down` conserva los volúmenes. No uses `down -v` salvo que quieras borrar permanentemente las bases y la caché de esta instalación.

El puerto publicado se limita a `0.0.0.0`: la API queda accesible desde cualquier equipo de la red.

## Incorporar cambios en Central.csv

Los CSV se copian dentro de la imagen de la API. Tras añadir o cambiar `data/Central.csv`, reconstruye la API desde esta carpeta:

```powershell
docker compose -p api_entregable up -d --build
```

Esto conserva los volúmenes. No borres matrículas ni volúmenes para forzar la importación: si MariaDB ya contiene datos, conserva esos datos y planifica una importación adicional por separado.
