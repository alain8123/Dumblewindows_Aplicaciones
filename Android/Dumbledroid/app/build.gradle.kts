// Configuración del módulo app: Android Studio la evalúa al sincronizar/compilar.
// Properties lee secrets.properties para generar BuildConfig.API_KEY; no se lee al pulsar Buscar.
import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
}

android {
    namespace = "com.dm2.dumbledroid"
    compileSdk {
        version = release(37)
    }

    defaultConfig {
        applicationId = "com.dm2.dumbledroid"
        minSdk = 30
        targetSdk = 37
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"

        // Archivo local, fuera del código fuente. No imprimir ni compartir su contenido.
        // Si no existe o no tiene API_KEY, se genera una cadena vacía (la autenticación puede fallar).
        val secrets = Properties().apply {
            val f = rootProject.file("secrets.properties")
            if (f.exists()) f.inputStream().use { load(it) }
        }
        buildConfigField("String", "API_KEY", "\"${secrets["API_KEY"] ?: ""}\"")
    }

    // Genera clases Binding para los layouts y BuildConfig para constantes de compilación.
    // Son archivos generados: se cambia esta configuración, no las clases generadas.
    buildFeatures{
        viewBinding = true
        buildConfig = true
    }

    buildTypes {
        release {
            optimization {
                enable = false
            }
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
}

dependencies {
    // Retrofit y convertidor Gson
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")
    // Gson convierte JSON a Estudiante y viceversa. Los permisos van en AndroidManifest.xml.
    implementation("com.google.code.gson:gson:2.8.8")

    // Coil (para cargar imágenes desde URL)
    implementation("io.coil-kt:coil:2.6.0")

    // Alias definidos en gradle/libs.versions.toml: interfaz Android y pruebas existentes.
    implementation(libs.androidx.activity.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.core.ktx)
    implementation(libs.material)
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.junit)
}
