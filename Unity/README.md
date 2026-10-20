# DumbleUnity

Aplicación para **Windows** y **Android** hecha en **Unity** que permite buscar a un estudiante de Hogwarts por su nombre y ver su **patronus** animado en pixel art.

Forma parte del proyecto **Dumblewindows**: los datos de los estudiantes vienen de la API de Dumblewindows y esta aplicación es la interfaz visual para consultarlos.

---

## Índice

1. [Qué hace la aplicación](#qué-hace-la-aplicación)
2. [Requisitos](#requisitos)
3. [Puesta en marcha](#puesta-en-marcha)
4. [Conectar con la API](#conectar-con-la-api)
5. [Compilar el proyecto](#compilar-el-proyecto)
6. [Estructura del proyecto](#estructura-del-proyecto)
7. [La escena](#la-escena)
8. [Flujo de una búsqueda](#flujo-de-una-búsqueda)
9. [Scripts](#scripts)
10. [Animaciones de los patronus](#animaciones-de-los-patronus)
11. [Mensajes de error](#mensajes-de-error)
12. [Cómo ampliar la aplicación](#cómo-ampliar-la-aplicación)
13. [Decisiones de diseño](#decisiones-de-diseño)
14. [Autor y derechos](#autor-y-derechos)

---

## Qué hace la aplicación

1. Muestra un menú con un vídeo animado del castillo de fondo.
2. El usuario escribe el **nombre** de un estudiante.
3. La aplicación consulta la API y enseña una **lista** con todos los estudiantes que coinciden.
4. Al elegir uno, se abre una ventana con su **patronus animado** y el nombre del animal.
5. Si algo falla (nombre no válido, sin conexión, estudiante sin patronus...), aparece un **aviso** que explica el problema.

---

## Requisitos

| Elemento | Versión / detalle |
|---|---|
| Unity | **6000.3.24f1** (Unity 6) |
| Render pipeline | Universal Render Pipeline (URP) 2D |
| Texto | TextMeshPro (incluido en `com.unity.ugui`) |
| Entrada | Input System (el nuevo sistema de entrada de Unity) |
| Módulos de Unity Hub | **Windows Build Support** para compilar en Windows y **Android Build Support** (con *OpenJDK* y *Android SDK & NDK Tools*) para Android |
| API | API de Dumblewindows: en local para Windows en el mismo equipo, o remota para Android (ver [Conectar con la API](#conectar-con-la-api)) |

---

## Puesta en marcha

1. Abre la carpeta `DumbleUnity` con **Unity Hub** usando la versión indicada arriba.
2. Abre la escena `Assets/Scenes/menu.unity`.
3. En la ventana **Hierarchy**, selecciona el objeto **ControladorConsultaAlumno**.
4. En la ventana **Inspector**, dentro del componente **Consulta Alumno (Script)**, rellena (paso a paso en [Dónde se introducen en el proyecto](#3-dónde-se-introducen-en-el-proyecto)):

   | Campo | Qué poner |
   |---|---|
   | **Url Base** | `http://127.0.0.1:8000/estudiantes` en Windows en el mismo equipo, o la URL remota en Android (ver [Conectar con la API](#conectar-con-la-api)) |
   | **Api Key** | La clave del archivo `.env` de la carpeta `API` |
   | **Nombre Cabecera** | Cabecera donde va la clave. Por defecto `X-API-Key` (lo indica Swagger en el botón *Authorize*) |
   | **Usar Bearer** | Actívalo solo si la API pide `Authorization: Bearer <clave>` |

5. Pulsa **Play**.

> **¿Por qué la URL y la clave no están escritas en el código?**
> Para que cada persona configure su propio servidor sin tocar el código, y para no subir la dirección ni la clave al repositorio. Si la URL está vacía, la app avisa con *"Falta la dirección de la API en el inspector"*.

---

## Conectar con la API

La aplicación no trae datos propios: todo lo consulta a la **API de Dumblewindows**. Los valores de **Url Base** y **Api Key** se sacan de esa API, no se inventan.

### 1. Url Base

Depende de dónde se ejecute DumbleUnity:

| Dónde se usa DumbleUnity | Url Base |
|---|---|
| **Windows / editor de Unity**, en el **mismo equipo** donde está montada la API | `http://127.0.0.1:8000/estudiantes` |
| **Android** | La **URL remota** donde está publicada la API, terminada en `/estudiantes` (por ejemplo `https://<servidor>/estudiantes`) |

- **API en local:** arráncala desde la carpeta `API` del repositorio; debe quedar escuchando en el **puerto 8000**. `127.0.0.1` significa "este mismo equipo", por eso solo sirve si la app y la API están en el mismo PC.
- **Android:** en un móvil, `127.0.0.1` sería el propio móvil, así que la versión de Android **solo funciona con la URL remota**. Compílala siempre con esa URL.

### 2. Api Key

La clave está en el archivo **`.env`** de la carpeta `API`. Ábrelo, copia el valor de la clave y pégalo en **Api Key**.

> El `.env` contiene la clave real: no debe subirse al repositorio ni compartirse.

### 3. Dónde se introducen en el proyecto

La URL y la clave **no se escriben en el código**: se rellenan en el **Inspector** de Unity.

1. Abre el proyecto en Unity y abre la escena **`Assets/Scenes/menu.unity`** (doble clic en la ventana **Project**).
2. En la ventana **Hierarchy** (la lista de objetos de la escena), haz clic en **`ControladorConsultaAlumno`**.
3. En la ventana **Inspector** aparece el componente **Consulta Alumno (Script)**. Rellena:
   - En el apartado **API** → campo **Url Base**: `http://127.0.0.1:8000/estudiantes` para Windows en el mismo equipo, o la URL remota para Android
   - En el apartado **Autenticación** → campo **Api Key**: la clave del `.env`
4. Guarda la escena con **Ctrl + S**. Si no la guardas, los valores se pierden al cerrar Unity.

```
Hierarchy                         Inspector
─────────────────────────         ───────────────────────────────────────
Main Camera                       ▼ Consulta Alumno (Script)
Global Light 2D                     UI
Canvas                                ...
EventSystem                         API
ControladorConsultaAlumno  ◄──        Url Base     [ http://127.0.0.1:8000/estudiantes ]
                                    Autenticación
                                      Nombre Cabecera  [ X-API-Key ]
                                      Api Key          [ <clave del .env> ]
                                      Usar Bearer      [ ]
```

Al pasar el ratón por encima de cada campo, Unity muestra una explicación (*Tooltip*).

En Swagger, el botón **Authorize** indica el nombre de la cabecera que espera la API. Si es distinto de `X-API-Key`, cámbialo en **Nombre Cabecera**. Si Swagger pide `Authorization: Bearer ...`, activa **Usar Bearer**.

---

## Compilar el proyecto

Las compilaciones se hacen desde **File → Build Profiles**. En **Scene List** debe estar marcada la escena `Scenes/menu`, que es la única de la aplicación.

> Antes de compilar, rellena **Url Base** y **Api Key** con los valores que corresponden a la plataforma: la API local para Windows en el mismo equipo y la URL remota para Android. Esos valores **se quedan dentro de la aplicación compilada**: si cambian, hay que volver a compilar.

### Windows

1. En **Build Profiles**, elige **Windows** y pulsa **Switch Platform** si no es la plataforma activa.
2. En **Player Settings** (botón del mismo panel) revisa:
   - **Company Name** (`Dumblewindows`) y **Product Name** (`DumbleUnity`): el `.exe` se llama como el producto.
   - **Resolution and Presentation → Fullscreen Mode**: la aplicación está pensada para 1920 × 1080 a pantalla completa.
3. Pulsa **Build** y elige una carpeta vacía, por ejemplo `Builds/Windows` (la carpeta `Builds` está en el `.gitignore` y no se sube al repositorio).
4. Se genera `DumbleUnity.exe` junto con la carpeta `DumbleUnity_Data`, `UnityPlayer.dll` y otros archivos. Para repartir la aplicación hay que copiar **la carpeta entera**: el `.exe` solo no funciona.

### Android

1. Instala desde **Unity Hub → Installs → (versión) → Add modules** el módulo **Android Build Support** con *OpenJDK* y *Android SDK & NDK Tools*, si no lo tienes.
2. En **Build Profiles**, elige **Android** y pulsa **Switch Platform**. La primera vez tarda, porque Unity reimporta los recursos.
3. El proyecto ya tiene configurado lo básico para Android:
   - **Package Name** (identificador único de la aplicación): `com.dumblewindows.dumbleunity`.
   - **Orientación**: horizontal (**Landscape Left**), porque la interfaz está diseñada en horizontal.
   - **IL2CPP** y **ARM64** (lo que exige Google Play), con Android 7.1 (API 25) como versión mínima.
4. Pon en **Url Base** la **URL remota** de la API (ver [Conectar con la API](#conectar-con-la-api)). Con `127.0.0.1` la versión de Android no puede conectarse.
5. Pulsa **Build** para generar un `.apk`, o conecta el móvil por USB con la **depuración USB** activada y pulsa **Build And Run** para instalarlo y abrirlo directamente.

> Para pruebas basta con la firma de depuración que Unity usa por defecto. Para publicar la aplicación hay que crear un *keystore* propio en **Player Settings → Publishing Settings** y guardarlo bien: sin él no se pueden publicar actualizaciones.

**Diferencias en Android:**
- No hay ratón, así que no existe el "pasar por encima". El recuadro de derechos de autor se ve **mientras se mantiene el dedo** sobre la etiqueta, y el efecto del botón de salir solo se nota al pulsarlo.
- El botón de salir cierra la aplicación igual que en Windows.

---

## Estructura del proyecto

```
Assets/
├── Prefabs/
│   └── UI/                      Prefabs de la interfaz, todos colocados en la escena
│       ├── BotonMenu.prefab              Botón "Buscar" del menú
│       ├── BotonSalir.prefab
│       ├── PopupAlumno.prefab
│       ├── PopupAviso.prefab
│       └── PopupSeleccionAlumno.prefab   Lista de estudiantes encontrados
├── Resources/                   Solo lo que se carga por nombre desde el código
│   └── PatronusPixelSprites/    Una tira de animación por patronus (ver más abajo)
├── Scenes/
│   └── menu.unity               Única escena de la aplicación
├── Scripts/                     Código C# (explicado en la sección Scripts)
├── Settings/                    Configuración de URP y del sistema de entrada (la usa Unity, no tocar)
├── Sprites/
│   └── boton_salir.png          Imagen del botón de salir
├── TextMesh Pro/                Fuentes, shaders y recursos de TextMeshPro
└── Videos/
    └── castillo_animado.mp4     Vídeo de fondo del menú
```

> **¿Por qué solo los patronus están en `Resources`?**
> Unity solo permite cargar archivos por su nombre (con `Resources.Load`) si están dentro de una carpeta llamada `Resources`. Los sprites de los patronus se eligen según lo que devuelve la API, así que tienen que estar ahí. El resto de recursos se arrastra directamente a la escena y no lo necesita. Además, todo lo que hay en `Resources` se incluye siempre en la build, por eso no conviene poner nada más.

---

## La escena

La escena `menu` tiene un único `Canvas` con todos los elementos de la interfaz:

```
Canvas
├── VideoFondo_Imagen          Vídeo del castillo a pantalla completa (VideoFondo)
├── Panel                      Menú principal: título, escudo, lema, campo de nombre y botón Buscar
├── BotonSalir                 Botón X abajo a la derecha (BotonSalir)
├── EtiquetaDerechos           "© Dumblewindows" abajo a la izquierda (EtiquetaDerechos)
│   └── RecuadroDerechos       Recuadro de derechos de autor (desactivado hasta pasar el ratón)
├── PopupAlumno                Ventana del patronus animado
├── PopupAviso                 Ventana de avisos y errores
└── PopupSeleccionAlumno       Lista de estudiantes encontrados

ControladorConsultaAlumno       Objeto con el script principal (ConsultaAlumno)
EventSystem                     Necesario para que la UI reciba clics y teclado
Main Camera / Global Light 2D   Cámara y luz de URP 2D
```

Las tres ventanas emergentes (`PopupAlumno`, `PopupAviso` y `PopupSeleccionAlumno`) están en la escena **desactivadas** y asignadas en el inspector de **ControladorConsultaAlumno**. Se activan al abrirlas y oscurecen y bloquean el `Panel` mientras están abiertas.

---

## Flujo de una búsqueda

```
Usuario escribe "HARRY"
        │
        ▼
ConsultaAlumno.AlPulsarBuscar()
   ├─ ¿Nombre vacío o no válido?          → PopupAviso
   ├─ ¿URL o cabecera mal configuradas?    → PopupAviso
        │
        ▼
GET <urlBase>?name=HARRY&limit=100        (corrutina, no congela la app)
        │
        ▼
ConsultaAlumno.GestionarRespuesta()
   ├─ Error HTTP (404, 401, sin conexión...) → PopupAviso
        │
        ▼
RespuestaAlumnosJson.Leer()               JSON → lista de estudiantes
        │
        ▼
PopupSeleccionAlumno                      El usuario elige un estudiante
        │
        ▼
ConsultaAlumno.MostrarAlumno()
   ├─ ¿Sin patronus o "Non-corporeal"?   → PopupAviso (o sprite no corpóreo)
        │
        ▼
PopupAlumno + PatronusSpriteAnimation     Patronus animado en bucle
```

---

## Scripts

Todos los scripts están en `Assets/Scripts`. Cada uno tiene documentación XML (`///`) que aparece como ayuda emergente en Visual Studio o Rider, y comentarios en casi cada línea.

### `ConsultaAlumno.cs` — el controlador principal

**Qué hace:** coordina toda la búsqueda: lee y valida el nombre, llama a la API, interpreta la respuesta y abre la ventana correspondiente.

**Cómo lo hace:**
- **Validación mientras se escribe.** Solo deja escribir letras (de cualquier idioma), espacios y apóstrofos, y lo pasa todo a mayúsculas. Usa dos mecanismos:
  - `onValidateInput` filtra cada tecla.
  - `onValueChanged` limpia el texto completo, para cubrir lo que se **pega** con Ctrl+V.
- **Validación final** con una expresión regular. Admite nombres como `MARÍA JOSÉ` u `O'BRIEN` y rechaza números, símbolos, espacios dobles o apóstrofos sueltos.
- **Petición HTTP** con `UnityWebRequest` dentro de una **corrutina**, para que la interfaz no se congele mientras espera. Tiene un tiempo límite de 10 segundos.
- **Autenticación** opcional por cabecera (`X-API-Key` u otra) o por `Authorization: Bearer`.
- **Protección contra dobles clics:** mientras hay una búsqueda en curso (`consultando = true`), el botón Buscar no hace nada.

**Por qué así:** validar antes de enviar evita peticiones inútiles y da mensajes claros al usuario. La corrutina es la forma estándar en Unity de esperar algo sin bloquear el juego.

**Campos del inspector:** todos tienen un *Tooltip* que se ve al pasar el ratón por encima. Además de los de la API, hay:
- **Animaciones personalizadas:** lista opcional "patronus → prefab" para sustituir el sprite de un patronus por una animación propia.
- **Valores sin patronus:** textos que significan "sin patronus corpóreo" (por defecto `Non-corporeal`).
- **Mostrar Logs:** escribe en la consola el código HTTP de cada respuesta.

### `RespuestaAlumnosJson.cs` — lector del JSON

**Qué hace:** convierte el texto JSON de la API en una lista de objetos `RespuestaApi` (nombre, apellido y patronus).

**Cómo y por qué:** `JsonUtility`, el lector de JSON de Unity, **no sabe leer una lista suelta** (`[ {...}, {...} ]`). El truco es envolverla en un objeto (`{"alumnos": [...]}`) antes de leerla. También acepta que la API devuelva un único estudiante (`{...}`). Si el texto no tiene estudiantes válidos, lanza una excepción que `ConsultaAlumno` convierte en el aviso *"Respuesta incorrecta"*.

### `PopupBase.cs` — base de todas las ventanas

**Qué hace:** contiene lo que comparten todas las ventanas emergentes:
- Se colocan **por encima de todo** y **bloquean** el menú de detrás.
- **Oscurecen** el menú de detrás mientras están abiertas.
- Aparecen y desaparecen con un **fundido y un pequeño zoom** de 0,15 segundos.

**Por qué:** así cada ventana solo programa lo suyo (rellenar sus textos) y todas se comportan igual. Usa tiempo real (`unscaledDeltaTime`) para que la animación funcione aunque el juego esté en pausa.

### `PopupAviso.cs` — ventana de avisos

Muestra un título y un mensaje. Se usa para todos los errores y avisos de la aplicación.

### `PopupSeleccionAlumno.cs` — lista de estudiantes

**Qué hace:** crea un botón por cada estudiante encontrado a partir de un botón "plantilla", dentro de una lista con barra de desplazamiento.

**Cómo y por qué:**
- No hace peticiones a la API: solo muestra la lista y **avisa a `ConsultaAlumno`** del estudiante elegido. Así cada clase tiene una sola responsabilidad.
- La respuesta de la búsqueda ya incluye el patronus, por lo que **no hace falta una segunda petición** al elegir.
- Se muestra **aunque solo haya un resultado**, para que el usuario confirme que es la persona que buscaba.
- La ventana se cierra *antes* de avisar de la elección. Si se cerrara con su fundido después, podría desbloquear el menú mientras ya está abierta la ventana del patronus.

### `PopupAlumno.cs` — ventana del patronus

**Qué hace:** muestra la animación del patronus sobre un fondo oscuro, con el nombre del animal debajo.

**Cómo:** si hay un prefab propio para ese patronus (configurado en *Animaciones personalizadas*), lo usa. Si no, usa `PatronusSpriteAnimation` con el spritesheet correspondiente. Si el objeto de animación no existe en la escena, lo crea por código.

### `PatronusSpriteAnimation.cs` — reproductor de spritesheets

**Qué hace:** carga la tira de imágenes de un patronus, la divide en 8 fotogramas y los reproduce en bucle a 10 fotogramas por segundo.

**Cómo:**
- **Normaliza el nombre** que llega de la API para encontrar el archivo: minúsculas, sin tildes y con `_` en lugar de espacios o guiones. Por ejemplo, `"Polar Bear"` se convierte en `polar_bear` y carga `polar_bear_pixel_8f.png`.
- Reconoce varias formas de escribir "no corpóreo" (`Non-corporeal`, `No corpóreo`...) y para todas usa el sprite `non_corporeal`.
- Libera los sprites al cambiar de patronus, para no acumular memoria.

**Por qué spritesheets y no un Animator:** añadir un patronus nuevo es tan sencillo como dejar un PNG con el nombre correcto en la carpeta, sin crear animaciones ni tocar código.

### `VideoFondo.cs` — vídeo de fondo

**Qué hace:** reproduce en bucle `castillo_animado.mp4` a pantalla completa.

**Cómo:** el `VideoPlayer` pinta cada fotograma en una `RenderTexture` que se crea con el **tamaño real del vídeo**, y un `RawImage` la muestra. Funciona también **en el editor sin pulsar Play** (`[ExecuteAlways]`), para ver el fondo mientras se diseña la escena. En el editor el vídeo va sin sonido.

### `BotonSalir.cs` — botón de salir

**Qué hace:** cierra la aplicación. Al pasar el ratón por encima, el botón crece un 12 % y se inclina un poco, como un sello que se levanta.

**Detalle:** dentro del editor, `Application.Quit()` no hace nada, así que en ese caso detiene el modo Play.

### `EtiquetaDerechos.cs` — aviso de derechos de autor

**Qué hace:** al pasar el ratón por encima de la etiqueta *"© Dumblewindows"*, activa el recuadro con el texto de derechos de autor, y lo desactiva al salir.

**Por qué tan simple:** el recuadro es un objeto normal de la escena. Su texto, colores y tamaño se editan en el inspector, y el script solo lo enciende y lo apaga.

---

## Animaciones de los patronus

### Formato

| Propiedad | Valor |
|---|---|
| Carpeta | `Assets/Resources/PatronusPixelSprites/` |
| Nombre del archivo | `<nombre_normalizado>_pixel_8f.png` |
| Tamaño | 960 × 120 px: **8 fotogramas** de 120 × 120 en una tira horizontal |
| Estilo | Pixel art azul "fantasmal", contorno oscuro y brillo exterior |
| Importación | Filtro **Point** (sin suavizado) y sin compresión |

### Patronus incluidos

`boar` · `cat` · `crocodile` · `deer` · `doe` · `dog` · `fox` · `goat` · `hare` · `hedgehog` · `horse` · `jack_russell_terrier` · `lioness` · `magpie` · `mink` · `non_corporeal` · `otter` · `owl` · `persian_cat` · `phoenix` · `polar_bear` · `raven_or_crow` · `rhinoceros` · `stag` · `swan` · `unicorn` · `weasel` · `wolf`

Todos los sprites están **centrados** en su fotograma. Si la API devuelve un patronus sin sprite, la ventana se abre con el recuadro vacío y aparece un aviso en la consola de Unity indicando qué archivo falta.

---

## Mensajes de error

| Situación | Aviso que ve el usuario |
|---|---|
| Campo vacío | *Nombre vacío* |
| Números o símbolos en el nombre | *Nombre no válido* |
| URL vacía o mal escrita en el inspector | *Configuración incorrecta* |
| HTTP 404 o lista vacía | *Alumno no encontrado* |
| HTTP 400 / 422 | *Nombre no válido* (la API rechazó la búsqueda) |
| HTTP 401 / 403 | *Acceso denegado* (clave de API incorrecta) |
| Sin conexión, servidor caído o más de 10 s sin respuesta | *Error de conexión* |
| JSON que no se puede leer | *Respuesta incorrecta* |
| Estudiante sin patronus | *Sin patronus* |

---

## Cómo ampliar la aplicación

### Añadir un patronus nuevo

1. Crea una tira PNG de 960 × 120 con 8 fotogramas de 120 × 120.
2. Llámala con el nombre normalizado del patronus: por ejemplo, para `"Jack Russell terrier"` usa `jack_russell_terrier_pixel_8f.png`.
3. Déjala en `Assets/Resources/PatronusPixelSprites/`.
4. En el inspector de la imagen, pon el **Filter Mode** en **Point** y la **Compression** en **None**, para que el pixel art se vea nítido.

No hace falta tocar código.

### Usar una animación propia para un patronus

1. Crea un prefab de UI (por ejemplo, un `Image` con un `Animator`).
2. En **ControladorConsultaAlumno → Animaciones personalizadas**, añade una entrada con el texto del patronus tal y como lo devuelve la API (por ejemplo, `Stag`) y el prefab.

### Cambiar el texto de derechos de autor

Edita el texto del objeto `TextoDerechos` (dentro de `EtiquetaDerechos → RecuadroDerechos`). Si el texto nuevo no cabe, ajusta el tamaño de `RecuadroDerechos`.

---

## Decisiones de diseño

| Decisión | Motivo |
|---|---|
| Búsqueda por nombre en lugar de ID | El usuario conoce el nombre, no el ID |
| Mostrar siempre la lista, aunque haya un único resultado | El usuario confirma a quién busca |
| Sin segunda petición al elegir un estudiante | La respuesta de la búsqueda ya incluye el patronus |
| URL y clave en el inspector | Configurables sin tocar el código y fuera del repositorio |
| Validación al escribir y al pulsar Buscar | Mensajes claros y menos peticiones inútiles |
| Corrutinas para las peticiones | La interfaz no se congela mientras espera |
| Una clase base para las ventanas | Todas se comportan igual y el código no se repite |
| Spritesheets cargados por nombre | Añadir un patronus es solo añadir un PNG |
| Animaciones con tiempo real (`unscaledDeltaTime`) | Funcionan aunque el juego esté en pausa |

---

## Autor y derechos

**Autor:** Ivan Salazar

© 2026 Dumblewindows.
