//import para poder leer la api_key en secret.properties
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

        //lee secret.properties para recoger la api_key
        val secrets = Properties().apply {
            val f = rootProject.file("secrets.properties")
            if (f.exists()) f.inputStream().use { load(it) }
        }
        buildConfigField("String", "API_KEY", "\"${secrets["API_KEY"] ?: ""}\"")
    }

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
    // Permisos y otras dependencias necesarias
    implementation("com.google.code.gson:gson:2.8.8")

    // Coil (para cargar imágenes desde URL)
    implementation("io.coil-kt:coil:2.6.0")

    implementation(libs.androidx.activity.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.core.ktx)
    implementation(libs.material)
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.junit)
}