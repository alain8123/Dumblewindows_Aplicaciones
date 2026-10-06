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