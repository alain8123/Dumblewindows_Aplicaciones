# Configuracion binding

Para poder usar "binding" al llamar a los objetos de la interfaz hacemos los siguientes pasos:

en la carpeta de "Gradle Scrips", el archivo "build.gradle.kts":

	dentro de "android":

		entre "defaultConfig" y "buildTypes" ponemos esto:

			buildFeatures{
        			viewBinding = true
    			}


sincronizamos gradle para aplicar los cambios y en cada archivo .kt añadimos lo siguiente:

	import com.dm2.dumbledroid.databinding.ActivityMainBinding
	
	encima de la función principal ponemos:

		private lateinit var binding: ActivityMainBinding

	dentro de "onCreate" dejamos con esta estructura:

		super.onCreate(savedInstanceState)
        	enableEdgeToEdge()
        	//setContentView(R.layout.activity_main)
        	binding = ActivityMainBinding.inflate(layoutInflater)
        	val view = binding.root
        	setContentView(view)


ahora podemos llamar al botón de búsqueda poniendo:
	
	"binding.Buscar" en vez de poner "val buscar = findViewById<Button>(R.id.Buscar)"


# Configuracion API

1. Configuración Inicial
Asegúrate de tener instalado el SDK de Android 11 (API 30) y de haber configurado las dependencias en tu archivo build.gradle (módulo app):

dependencies {
    // Retrofit y convertidor Gson
    implementation 'com.squareup.retrofit2:retrofit:2.9.0'
    implementation 'com.squareup.retrofit2:converter-gson:2.9.0'
    
    // Permisos y otras dependencias necesarias
    implementation 'com.google.code.gson:gson:2.8.8'
}

- Pulsamos el boton de sincronizar el gradle

2. Permisos de Red
Debes agregar el permiso de internet en el archivo AndroidManifest.xml:

<uses-permission android:name="android.permission.INTERNET" />

