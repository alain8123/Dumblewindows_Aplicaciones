# DumbleUnity

Aplicación de escritorio hecha en **Unity** que permite buscar a un estudiante de Hogwarts por su nombre y ver su **patronus** animado en pixel art.

Forma parte del proyecto **Dumblewindows**: los datos de los estudiantes vienen de la API de Dumblewindows y esta aplicación es la interfaz visual para consultarlos.

---

## Índice

1. [Qué hace la aplicación](#qué-hace-la-aplicación)
2. [Requisitos](#requisitos)
3. [Puesta en marcha](#puesta-en-marcha)
4. [Estructura del proyecto](#estructura-del-proyecto)
5. [La escena](#la-escena)
6. [Flujo de una búsqueda](#flujo-de-una-búsqueda)
7. [Scripts](#scripts)
8. [Animaciones de los patronus](#animaciones-de-los-patronus)
9. [Mensajes de error](#mensajes-de-error)
10. [Cómo ampliar la aplicación](#cómo-ampliar-la-aplicación)
11. [Decisiones de diseño](#decisiones-de-diseño)
12. [Autor y derechos](#autor-y-derechos)

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
| API | API de Dumblewindows accesible por HTTP/HTTPS |

---

## Puesta en marcha

1. Abre la carpeta `DumbleUnity` con **Unity Hub** usando la versión indicada arriba.
2. Abre la escena `Assets/Scenes/menu.unity`.
3. En la jerarquía, selecciona el objeto **ControladorConsultaAlumno**.
4. En el inspector, dentro del componente **Consulta Alumno**, rellena:

   | Campo | Qué poner |
   |---|---|
   | **Url Base** | Dirección de la colección de estudiantes de la API, por ejemplo `https://<servidor>/estudiantes/` |
   | **Api Key** | La clave de la API (si la API la pide) |
   | **Nombre Cabecera** | Cabecera donde va la clave. Por defecto `X-API-Key` (lo indica Swagger en el botón *Authorize*) |
   | **Usar Bearer** | Actívalo solo si la API pide `Authorization: Bearer <clave>` |

5. Pulsa **Play**.

> **¿Por qué la URL y la clave no están escritas en el código?**
> Para que cada persona configure su propio servidor sin tocar el código, y para no subir la dirección ni la clave al repositorio. Si la URL está vacía, la app avisa con *"Falta la dirección de la API en el inspector"*.

---

## Estructura del proyecto

```
Assets/
├── Prefabs/
│   └── UI/                      Prefabs de la interfaz, todos colocados en la escena
│       ├── BotonMenu.prefab
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
