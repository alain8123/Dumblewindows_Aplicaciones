# DumbleFX — Guía de la aplicación

Aplicación de escritorio desarrollada con **JavaFX** para consultar estudiantes y gestionar matrículas mediante la API **DumbleWindows**. Su interfaz está ambientada en un castillo mágico inspirado en Hogwarts y permite trabajar en español, inglés y euskera.

Este documento explica **qué hace cada parte, cómo funciona y por qué se utiliza**. Se entrega como descarga independiente: no sustituye el README existente ni modifica el proyecto.

## 1. Funcionalidades

| Sección | Qué permite hacer |
| --- | --- |
| Consulta por ID | Localizar un estudiante mediante su identificador y seleccionar una coincidencia si hay varias. |
| Consulta por datos | Buscar por nombre y casa con un máximo de resultados. |
| Crear estudiante | Registrar una matrícula con nombre, apellidos, casa y fecha opcional. |
| Actualizar estudiante | Cargar una matrícula y guardar cambios en sus datos. |
| Eliminar estudiante | Consultar una matrícula y eliminarla tras confirmación. |

La aplicación es un **cliente**: envía peticiones a la API y presenta sus respuestas. No se conecta directamente a MariaDB. Las pantallas describen las escrituras como operaciones sobre matrículas de MariaDB, no sobre estudiantes históricos; esa restricción también debe aplicarla el servidor.

## 2. Tecnologías y propósito

Versiones declaradas en el `pom.xml` revisado:

| Tecnología | Versión | Cómo se utiliza y por qué |
| --- | --- | --- |
| Java | 21 | Lenguaje principal y cliente HTTP del JDK. |
| JavaFX | 21.0.6 | Ventanas, controles, formularios, tablas y diálogos. |
| FXML | JavaFX | Separa la estructura visual de la lógica de las operaciones. |
| CSS de JavaFX | — | Centraliza colores, fuentes, fondos y estados visuales. |
| Jackson Databind | 2.17.2 | Convierte objetos Java a JSON y respuestas JSON a modelos Java. |
| Maven y wrapper | — | Gestionan dependencias, compilación y ejecución. |
| ResourceBundle | JDK | Organiza los textos por idioma. |

Esta separación facilita cambiar una pantalla sin mezclar su diseño con el código HTTP o las reglas de validación.

## 3. Estructura del proyecto

```text
JavaFX/
├── pom.xml
├── mvnw / mvnw.cmd
└── src/main/
    ├── java/Dumblewindows/javafx/dumblefx/
    │   ├── Launcher.java
    │   ├── Initialize.java
    │   ├── ConfigDialog.java
    │   ├── API/
    │   │   ├── ApiConfig.java
    │   │   └── ApiClient.java
    │   ├── controllers/
    │   │   ├── MainController.java
    │   │   └── StudentController.java
    │   ├── assets/
    │   │   ├── InputValidation.java
    │   │   └── StudentIdPreview.java
    |   ├──Modelos/
    |   |     ├──Alumno.java
    |   |     └──Matricula.java
    |   | 
    │   └── I18n/I18n.java
    └── resources/Dumblewindows/javafx/dumblefx/
        ├── config.properties
        ├── estilos/app.css
        ├── i18n/messages_es.properties
        ├── i18n/messages_en.properties
        ├── i18n/messages_eu.properties
        ├── icono/
        ├── imagenes/
        └── ventanas/
            ├── hello-view.fxml
            ├── consulta-id.fxml
            ├── consulta-datos.fxml
            ├── crear-estudiante.fxml
            ├── actualizar-estudiante.fxml
            └── eliminar-estudiante.fxml
```

### Responsabilidad de las clases

- **`Launcher`:** punto de entrada configurado para ejecutar la aplicación.
- **`Initialize`:** prepara la configuración, carga la vista principal, aplica traducciones, estilos e iconos y muestra la ventana.
- **`ConfigDialog`:** solicita los datos iniciales y permite editarlos con el botón **Configuración** del menú lateral.
- **`ApiConfig`:** localiza y carga las propiedades, consulta variables de entorno y resuelve las URLs.
- **`ApiClient`:** construye peticiones HTTP, envía la clave y procesa respuestas y errores.
- **`MainController`:** cambia la sección visible, su título, la ayuda y el botón activo del menú.
- **`StudentController`:** valida formularios, coordina las operaciones y muestra resultados.
- **`Alumno`:** modelo de los datos recibidos en las consultas.
- **`Matricula`:** modelo utilizado para solicitudes y respuestas de creación y actualización.
- **`InputValidation`:** reúne las reglas reutilizables de nombres e identificadores.
- **`StudentIdPreview`:** calcula la vista previa del ID durante el alta.
- **`I18n`:** obtiene traducciones y vincula textos al idioma seleccionado.

Los FXML definen los controles y su distribución; los controladores responden a las acciones; los modelos representan los datos; la capa API realiza la comunicación externa.

## 4. Arranque y configuración

### Flujo de inicio

1. Se ejecuta `Launcher`.
2. `Initialize` prepara la configuración mediante `ConfigDialog`.
3. Si falta, se muestra el formulario inicial antes de abrir la ventana principal.
4. Guardar correctamente permite continuar. Cancelar o cerrar ese formulario termina la aplicación.
5. Se carga `hello-view.fxml`, se aplican estilos e iconos y se muestra la ventana.
6. El controlador abre inicialmente la consulta por ID.

**Por qué:** evita abrir el espacio de trabajo sin haber preparado los datos de conexión.

### Archivo de configuración

En desarrollo, ejecutando desde la raíz del proyecto, la ruta predeterminada es:

```text
Ruta donde esta alojado el proyecto\Dumblewindows_Aplicaciones\JavaFX\src\main\resources\Dumblewindows\javafx\dumblefx\config.properties
```

El código calcula `src/main/resources/Dumblewindows/javafx/dumblefx/config.properties` **respecto al directorio de trabajo**.

Se puede indicar otro archivo mediante esta opción de la JVM:

```text
-Ddumblefx.config=C:\ruta\config.properties
```

`ApiConfig` contempla una configuración incluida como recurso si no se indica una ruta externa. Sin embargo, el `pom.xml` actual excluye los archivos `config.properties` de los recursos empaquetados. Para distribuir la aplicación conviene utilizar un archivo externo.

### Variables

| Variable | Función |
| --- | --- |
| `DUMBLEFX_API_URL` | URL base del servidor. |
| `DUMBLEFX_API_KEY` | Clave enviada en la cabecera `X-API-Key`. |
| `DUMBLEFX_API_URL_CONSULTA_ID` | URL opcional de consulta por ID. |
| `DUMBLEFX_API_URL_CONSULTA_DATOS` | URL opcional de búsqueda. |
| `DUMBLEFX_API_URL_CREAR` | URL opcional de creación. |
| `DUMBLEFX_API_URL_ELIMINAR` | URL opcional de eliminación. |
| `DUMBLEFX_API_URL_ACTUALIZAR` | URL opcional de actualización. |

Las cinco propiedades opcionales se guardan aunque estén vacías. Los ejemplos del formulario quedan como comentarios, no como valores de conexión.

Ejemplo ilustrativo, sin credenciales reales:

```properties
DUMBLEFX_API_URL=https://api.ejemplo.invalid
DUMBLEFX_API_KEY=REEMPLAZAR_POR_TU_CLAVE

# Ejemplo: https://api.ejemplo.invalid/estudiantes/{student_id}
DUMBLEFX_API_URL_CONSULTA_ID=
# Ejemplo: https://api.ejemplo.invalid/estudiantes
DUMBLEFX_API_URL_CONSULTA_DATOS=
# Ejemplo: https://api.ejemplo.invalid/matriculas
DUMBLEFX_API_URL_CREAR=
# Ejemplo: https://api.ejemplo.invalid/matriculas/{student_id}
DUMBLEFX_API_URL_ELIMINAR=
# Ejemplo: https://api.ejemplo.invalid/matriculas/{student_id}
DUMBLEFX_API_URL_ACTUALIZAR=
```

Si una URL opcional está vacía, se utiliza la URL base más la ruta de la operación. Si se indica, tiene prioridad. Las URLs específicas de consulta por ID, actualización y eliminación deben conservar literalmente `{student_id}`.

Las variables de entorno **no vacías** del mismo nombre prevalecen sobre el archivo. Los cambios guardados desde **Configuración** se aplican a las siguientes peticiones sin reiniciar. Si editas el archivo externamente o cambias el entorno, reinicia la aplicación.

**Seguridad:** el formulario oculta la clave al introducirla, pero el archivo la almacena en texto plano. No lo publiques ni incluyas la clave en capturas. Una clave en un cliente de escritorio no equivale a autenticación individual por usuario. Validar una URL no comprueba la disponibilidad del servidor ni la validez de la clave.

## 5. Pantallas: qué hacen, cómo y por qué

### 5.1. Ventana principal y navegación

`hello-view.fxml` contiene la estructura general. El menú cambia la vista central sin crear una ventana principal diferente para cada operación.

El botón de la sección abierta conserva un tono más claro aunque el foco pase a un campo. `MainController` activa la pseudoclase `selected` y el CSS define su apariencia.

**Por qué:** distingue la sección activa sin depender del último control que recibió el foco.

### 5.2. Consulta por ID

Se introduce un identificador válido y se solicita la información a la API. Si hay una coincidencia, se muestran sus datos; si hay varias, aparece un diálogo para seleccionar un estudiante. Cancelar no carga ninguno.

**Por qué:** un identificador base puede tener varias coincidencias y no debe tratarse automáticamente la primera como la correcta.

### 5.3. Consulta por datos

Permite buscar por nombre y casa, ambos opcionales, con un máximo de entre **1 y 100** resultados. La tabla muestra ID, nombre, apellidos, casa y nacimiento.

Los filtros se envían al servidor; no se descarga toda la información para filtrarla localmente.

**Por qué:** limita los datos transferidos y deja la búsqueda en el sistema que posee los registros.

### 5.4. Crear estudiante o matrícula

Se rellenan nombre y apellidos por separado y, opcionalmente, la fecha de nacimiento. En el alta, la casa permanece como `Central` y el ID se calcula automáticamente como vista previa. ID y casa están bloqueados en este formulario.

Ejemplo:

```text
Casa: Central
Nombre: Harry
Apellidos: Potter Evans
ID previsto: CE-HAR-POT
```

La vista previa usa abreviaturas de casa, nombre y primer apellido, en mayúsculas y sin tildes. **No reserva el identificador ni garantiza que esté libre.** La API devuelve el ID definitivo y puede añadir un sufijo como `-02` si corresponde.

Ejemplo del cuerpo enviado:

```json
{
  "full_name": "Harry",
  "apellido": "Potter Evans",
  "house": "Central",
  "birth_date": "2000-01-31"
}
```

Si no hay fecha, se omite del JSON. El cliente no impone el ID de la vista previa en el alta. Tras crear correctamente, limpia los datos personales y muestra el ID devuelto.

**Por qué:** la vista previa orienta al usuario y el servidor conserva la responsabilidad de asignar el identificador definitivo.

### 5.5. Actualizar estudiante

1. Introduce un ID y carga sus datos.
2. Si hay varias coincidencias, selecciona una.
3. Edita nombre, **apellidos**, casa o nacimiento.
4. Guarda los cambios.

El formulario incluye apellidos entre nombre y casa. Se conserva el ID completo elegido y se exige cargar la matrícula antes de actualizarla. Si se cambia el campo a otro ID, es necesario cargarlo antes de guardar.

La solicitud envía los datos de la matrícula, no solamente el campo editado. Si la respuesta devuelve un nuevo ID, la interfaz lo conserva.

**Por qué:** evita actualizar un registro distinto del revisado y mantiene los apellidos en el envío.

### 5.6. Eliminar estudiante

Primero se consulta el ID. Cuando hay varias coincidencias, aparece el selector y se cargan los datos de la matrícula elegida. El ID que se elimina debe coincidir con el cargado. Después se solicita confirmación final.

Cancelar la selección o la confirmación no elimina nada. Un ID distinto al cargado requiere una nueva consulta antes del borrado.

**Por qué:** la consulta, la selección y la confirmación reducen el riesgo de borrar una matrícula equivocada.

## 6. Validaciones

### IDs

Formatos completos admitidos:

```text
CE-HAR-POT
CE-HAR-POT-02
```

Expresión regular utilizada:

```regex
[A-Za-z]{2}-[A-Za-z]{3}-[A-Za-z]{3}(?:-[0-9]{2})?
```

Son dos letras, guion, tres letras, guion y tres letras, con un sufijo opcional de guion y dos cifras. Las letras son ASCII, mayúsculas o minúsculas. Los guiones son los únicos símbolos admitidos.

Hay dos controles complementarios:

- **Al escribir o pegar:** se permiten letras ASCII, cifras y guiones, hasta 13 caracteres, para poder construir el identificador progresivamente.
- **Antes de operar:** se exige el formato completo. Un ID incompleto no se envía.

### Nombres y apellidos

Admiten letras Unicode, marcas de acentuación y espacios, con un máximo de **200 caracteres por campo**. Nombre y apellidos son obligatorios en alta y actualización.

No admiten números, apóstrofos, guiones ni otros símbolos. El filtro también se aplica al pegar. La búsqueda por nombre usa el filtro de caracteres, pero puede dejarse vacía.

### Otros campos

- **Casa:** hasta 80 caracteres al guardar; en el alta se mantiene `Central`.
- **Nacimiento:** opcional, elegido con calendario; no puede ser posterior al día actual.
- **Límite de resultados:** entero de 1 a 100.

**Por qué:** se detectan errores antes de hacer peticiones innecesarias. Esto no sustituye la validación del servidor. Si otro sistema devuelve un ID con un formato diferente, la regla actual de la interfaz requeriría una adaptación para operarlo.

## 7. Comunicación con la API

| Operación | Método | Ruta predeterminada |
| --- | --- | --- |
| Consulta por ID | `GET` | `/estudiantes/{student_id}` |
| Búsqueda por datos | `GET` | `/estudiantes` |
| Creación | `POST` | `/matriculas` |
| Actualización | `PUT` | `/matriculas/{student_id}` |
| Eliminación | `DELETE` | `/matriculas/{student_id}` |

`ApiClient` utiliza `HttpClient` y Jackson. Añade `Accept: application/json`, `X-API-Key` y, al enviar un cuerpo, el tipo de contenido JSON. Codifica identificadores y filtros para construir las URLs.

Las búsquedas envían `name`, `house` y `limit`, según corresponda. La consulta por ID acepta un objeto o una lista; la búsqueda por datos espera una lista.

### Tareas en segundo plano

`StudentController` utiliza `Task` y un hilo de trabajo para las llamadas de red. Mientras una operación está en marcha, deshabilita el panel de operación y muestra el estado de conexión.

**Por qué:** la espera de red no debe bloquear el hilo de la interfaz ni permitir envíos repetidos desde ese panel durante la petición.

El cliente establece un tiempo de conexión de **8 segundos** y un tiempo máximo de petición de **20 segundos**. Ante un error HTTP intenta mostrar el código y el detalle del servidor. Si detecta la clave en ese mensaje de error, la sustituye por un texto de ocultación.

## 8. Idiomas, tooltips y ayuda

El selector permite cambiar entre **español, inglés y euskera**. `I18n` mantiene el idioma activo y carga los archivos `messages_es.properties`, `messages_en.properties` y `messages_eu.properties`.

Los textos vinculados al idioma cambian sin reiniciar: etiquetas, títulos, mensajes, columnas y ayudas de los controles preparados para ello.

- **Círculo `?`:** explica la sección abierta al pasar el ratón.
- **Círculo `©`:** presenta los créditos y el propósito mediante un tooltip.
- **Botones:** incluyen ayudas para consultar, cargar, crear, actualizar, seleccionar, confirmar o cancelar.

Los tooltips envuelven el texto y se muestran tras una espera breve de unos 200 milisegundos.

**Por qué:** separar traducciones de la lógica facilita añadir o corregir textos y mantener ayudas coherentes con la sección visible.

## 9. Diseño visual

`estilos/app.css` centraliza la apariencia. El fondo utiliza `imagenes/hogwarts-background.jpg`, centrado, sin repetición y con ajuste `cover`: cubre el área disponible y puede recortar los bordes según la proporción de la ventana.

El menú oscuro y los formularios claros semitransparentes mantienen contraste sobre el fondo. La imagen está incluida como recurso y no necesita descargarse al arrancar.

Los tamaños de letra se expresan en **`em`**, relativos al tamaño tipográfico de referencia. Esto conserva proporciones entre títulos y textos. **Usar `em` no adapta por sí solo toda la interfaz a cualquier pantalla**: también influyen la escala del sistema, los contenedores y sus restricciones.

La ventana revisada comienza en **1100 × 700** y establece esas mismas dimensiones como mínimo.

## 10. Ejecución y compilación

### Requisitos

- JDK 21 configurado para el proyecto.
- Acceso al servidor de la API y una clave válida.
- Conexión para descargar dependencias si aún no están disponibles localmente.

### Desde IntelliJ IDEA

1. Abre el proyecto y configura el SDK de Java 21.
2. Importa o recarga Maven.
3. Selecciona la raíz `JavaFX` como directorio de trabajo de la ejecución.
4. Ejecuta `Dumblewindows.javafx.dumblefx.Launcher`.
5. Completa el formulario inicial si falta configuración.

### Desde Windows

Desde una terminal situada en la raíz del proyecto:

```powershell
.\mvnw.cmd javafx:run
```

Para compilar y empaquetar:

```powershell
.\mvnw.cmd clean package
```

Si Maven no encuentra Java, revisa el JDK y `JAVA_HOME`. Estos comandos documentan el uso del proyecto: preparar este README no incluye una compilación nueva ni una prueba visual.

### Ejecutar el fatjar

Un *fatjar* es un único `.jar` que incluye la aplicación y todas sus dependencias (JavaFX y Jackson), por lo que se puede ejecutar sin Maven. Se genera con `mvnw clean package` y queda en la carpeta `target/`. El nombre exacto depende del `artifactId`, la versión y la configuración del `pom.xml` (por ejemplo `target/dumblefx-1.0-SNAPSHOT-fat.jar`); en los ejemplos se usa `dumblefx-fat.jar` como nombre ilustrativo. Sustitúyelo por el real.

**Requisito:** JDK o JRE 21 instalado. Compruébalo con:

```text
java -version
```

#### Si no tienes Java instalado

Si `java -version` responde que el comando no existe o muestra una versión inferior a 21, instala Java 21 antes de ejecutar el jar. Para solo ejecutar el fatjar basta un JRE; para compilar el proyecto con Maven hace falta el JDK, por eso aquí se instala el JDK.

**Windows**

Opción A, con `winget` (incluido en Windows 10 y 11 actuales), desde PowerShell:

```powershell
winget install EclipseAdoptium.Temurin.21.JDK
```

Opción B, con instalador: descarga el paquete `.msi` de Temurin 21 desde <https://adoptium.net>, ejecútalo y, en el asistente, activa las opciones **Set JAVA_HOME variable** y **Add to PATH**.

Cierra y vuelve a abrir la terminal (el PATH no se actualiza en las ventanas ya abiertas) y comprueba:

```powershell
java -version
```

**Linux**

Instala el paquete de OpenJDK 21 con el gestor de tu distribución:

```bash
# Debian / Ubuntu / Linux Mint
sudo apt update && sudo apt install openjdk-21-jdk
```

Si tu versión de la distribución no ofrece Java 21 (por ejemplo, Debian 12), instala Temurin 21 desde <https://adoptium.net> o usa [SDKMAN!](https://sdkman.io):

```bash
curl -s "https://get.sdkman.io" | bash
source "$HOME/.sdkman/bin/sdkman-init.sh"
sdk install java 21-tem
```

Comprueba la instalación:

```bash
java -version
```

Si hay varias versiones de Java instaladas y `java -version` no muestra la 21, selecciónala con `sudo update-alternatives --config java` (Debian/Ubuntu) o `sudo alternatives --config java` (Fedora/RHEL).

#### En Windows (PowerShell o CMD)
Al descargar la version del github podras ejecutar este comando o hacerle doble click .
```powershell
java -jar ruta\dumblefx_v1_.jar
```

Con un archivo de configuración externo (las opciones de la JVM van **antes** de `-jar`):

```powershell
java -Ddumblefx.config=C:\ruta\config.properties -jar ruta/dumbledumbleFX_v1.jar
```

Con la clave y la URL por variables de entorno, solo para esa sesión de PowerShell:

```powershell
$env:DUMBLEFX_API_URL = "https://api.ejemplo.invalid"
$env:DUMBLEFX_API_KEY = "REEMPLAZAR_POR_TU_CLAVE"
java -jar ruta/dumbleFX_v1.jar
```

Si la ruta contiene espacios, enciérrala entre comillas. Para abrir la aplicación sin ventana de consola, usa `javaw` en lugar de `java`.

#### En Linux (bash)

```bash
java -jar ruta\dumbleFX_v1.jar
```

Con variables de entorno:

```bash
export DUMBLEFX_API_URL="https://api.ejemplo.invalid"
export DUMBLEFX_API_KEY="REEMPLAZAR_POR_TU_CLAVE"
java -jar ruta\dumbleFX_v1.jar
```

No hace falta `chmod +x` para ejecutarlo con `java -jar`. JavaFX necesita un entorno gráfico (X11 o Wayland), así que no funciona en un servidor sin escritorio. Si falla por bibliotecas gráficas ausentes, instala GTK 3 con el gestor de paquetes de tu distribución (por ejemplo `sudo apt install libgtk-3-0`).

#### Notas comunes

- **Configuración:** `config.properties` no se empaqueta dentro del jar (el `pom.xml` lo excluye). Usa `-Ddumblefx.config=...` o variables de entorno; si no, la aplicación busca el archivo respecto al directorio desde el que lanzas el comando y mostrará el formulario inicial.
- **Seguridad:** no escribas la clave real en scripts ni en el historial compartido de la terminal si el equipo lo usan otras personas.
- Si aparece el error *«faltan componentes de JavaFX»*, el jar no es un fatjar completo o se ha arrancado una clase que extiende `Application` directamente. El punto de entrada debe ser `Launcher`, declarado como `Main-Class` en el manifiesto.

## 11. Problemas habituales

| Problema | Qué revisar |
| --- | --- |
| Reaparece el formulario inicial | Directorio de trabajo, ubicación del archivo y opción `dumblefx.config`. |
| No se guarda la configuración | Permisos de escritura y ruta de destino. |
| No conecta con la API | URL, red, disponibilidad del servidor y clave. |
| Falla una URL específica por ID | Presencia literal de `{student_id}`. |
| Se rechaza un ID | Estructura de dos, tres y tres letras, con sufijo opcional de dos cifras. |
| No deja actualizar o borrar | Haber cargado y seleccionado el mismo ID del campo. |
| Hay varias coincidencias | Elegir el registro concreto en el diálogo. |
| Falla un FXML | Ruta, controlador, `fx:id` e importaciones de controles como `Tooltip`. |
| Falta una traducción | Misma clave presente en los tres archivos de idioma. |
| No aparece el fondo | Ruta e inclusión de la imagen como recurso. |

## 12. Dónde realizar cambios

| Cambio deseado | Parte correspondiente |
| --- | --- |
| Distribución de una pantalla | Su archivo FXML. |
| Colores, fuentes y fondo | `app.css` y recursos de imagen. |
| Textos y tooltips | Archivos de traducción y sus vínculos. |
| Reglas de nombres e IDs | `InputValidation` y comprobaciones de `StudentController`. |
| Vista previa del ID | `StudentIdPreview`. |
| Contrato HTTP | `ApiClient`, `Alumno` y `Matricula`. |
| Lectura y ubicación de configuración | `ApiConfig` y `ConfigDialog`. |
| Autores de los créditos | Constante `AUTHORS` de `MainController`. |

Antes de distribuir, comprueba las cinco pantallas, los tres idiomas, el arranque sin configuración, las selecciones con varias coincidencias y las confirmaciones. Mantén claves y datos personales fuera de la documentación pública.

---

Documentación preparada a partir de los archivos revisados del proyecto el **15 de octubre de 2026**. Describe el cliente JavaFX; no certifica la implementación interna ni la disponibilidad del servidor de la API.

## 13. Documentación del código y Javadoc

Las clases Java incorporan Javadoc de responsabilidad, métodos, parámetros, resultados y errores declarados; los campos documentan su papel. Los comentarios internos explican validaciones, trabajo en segundo plano y decisiones de interfaz sin cambiar la lógica. Las vistas FXML y los estilos CSS incluyen comentarios de orientación.

Para generar el sitio Javadoc desde la raíz del proyecto con JDK 21 y Maven:

```powershell
.\mvnw.cmd javadoc:javadoc -Dshow=private -Ddocencoding=UTF-8 -Dencoding=UTF-8 -Dcharset=UTF-8
```

Abre `target/site/apidocs/index.html` al terminar. Maven puede necesitar descargar el plugin de Javadoc la primera vez. También puedes generarlo con la herramienta `javadoc` del JDK y las dependencias del proyecto en el classpath.

En Actualizar, la casa permanece **Central**, deshabilitada y no editable en el FXML; al cargar un alumno el controlador vuelve a establecer ese valor. Esta documentación no añade un selector de patronus ni modifica el contrato de la API.

## 14. Configuración desde el menú y revisión de ventanas 

El botón **Configuración**, situado al pie del menú lateral, abre un formulario modal como el de la referencia. Permite revisar la URL base, la clave oculta y las cinco URLs de operación sin abandonar la pantalla actual.

- **Guardar cambios** valida HTTP/HTTPS, los campos obligatorios y `{student_id}` en las URLs específicas de consulta por ID, eliminación y actualización. Las URLs opcionales pueden quedar vacías para usar la URL base y la ruta predeterminada.
- Se precargan las propiedades del archivo y se conservan otras propiedades que no aparecen en el formulario. Las variables de entorno siguen teniendo prioridad; editar el archivo no las modifica.
- La escritura utiliza un temporal y un reemplazo atómico cuando el sistema lo permite. La configuración activa solo se actualiza si el guardado termina correctamente. Las peticiones que ya estaban en curso no se reinician.
- **Cancelar** o cerrar el diálogo no escribe cambios. Durante el primer arranque, cancelar sigue cerrando la aplicación.
- La clave permanece oculta en pantalla, pero se guarda en texto plano. No compartas `config.properties` ni lo incluyas en el repositorio.

`ConfigDialog.editConfiguration`, el manejador `MainController.configure` y los nuevos métodos de `ApiConfig` incorporan Javadoc. `saveInitialConfiguration` sigue protegiendo los archivos existentes; `saveConfiguration` permite reemplazarlos y actualizar la instantánea de configuración.

### Ventanas, FXML y eventos

Los métodos `onAction="#..."` deben existir en el controlador declarado por `fx:controller` y llevar `@FXML` si son privados. Las vistas deben abrirse con `FXMLLoader` y el catálogo de traducciones; abrir un FXML aislado sin ese catálogo no reproduce el arranque normal.

Se unifica el namespace de las vistas de alta y consulta por datos a JavaFX 21, la versión utilizada por Maven, en lugar de JavaFX 26. También se elimina del arranque la solicitud del icono de 512 píxeles, porque no existe entre los recursos del proyecto y provocaba una excepción antes de mostrar la ventana.

Si IntelliJ sigue marcando un `onAction` pero la aplicación carga esa vista correctamente, recarga Maven y comprueba el SDK 21 del proyecto y del ejecutor Maven. Ese aviso del editor debe distinguirse de una excepción real de `FXMLLoader`; el texto y la traza de la excepción permiten localizar el control afectado.
