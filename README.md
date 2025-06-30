# High Risk Entity Searcher API

Esta es una API REST desarrollada en .NET 9 como solución a una prueba técnica para un puesto de .NET Developer. El servicio busca de forma concurrente entidades en múltiples listas de alto riesgo y consolida los resultados en una única respuesta.

## Características

-   **Búsqueda Concurrente:** Realiza búsquedas en todas las fuentes de datos en paralelo para una respuesta más rápida.
-   **Arquitectura Limpia:** Sigue principios de diseño SOLID, utilizando una arquitectura en capas (Controladores, Servicios, Clientes) y Inyección de Dependencias.
-   **Scraping Robusto:** Utiliza `Playwright` para controlar un navegador headless, permitiendo la interacción con sitios web complejos que requieren ejecución de JavaScript o envío de formularios.
-   **Manejo de APIs Internas:** Demuestra la capacidad de interceptar y consumir APIs JSON internas de un sitio web para una extracción de datos más fiable que el scraping de HTML.
-   **Pruebas de Integración:** Incluye un proyecto de pruebas (`xUnit`) con tests de integración que validan el comportamiento completo de la API.
-   **Manejo de Fallos:** Implementa una estrategia de fallback con datos simulados para fuentes que están protegidas contra el scraping.

## Fuentes de Datos Implementadas

-   **The World Bank Debarred List:** Se utiliza `Playwright` para interceptar la llamada a la API JSON interna del sitio.
-   **OFAC Sanctions List:** Se utiliza `Playwright` para automatizar el llenado del formulario de búsqueda y extraer los resultados de la tabla HTML.
-   **Offshore Leaks Database (ICIJ):** Se intentó el acceso con `HttpClient` y `Playwright`, pero la fuente está protegida por un sistema anti-bots (CAPTCHA). El cliente implementa un fallback a datos simulados para demostrar la funcionalidad.

---

## Cómo Ejecutar el Proyecto

### Prerrequisitos

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (o superiores)
-   Un editor de código como VS Code o Zed.

### Pasos para la Ejecución

1.  **Clonar el Repositorio**
    ```bash
    git clone https://github.com/Lenas25/HighRiskEntitySearcher.git  
    cd HighRiskEntitySearcher
    ```

2.  **Restaurar Dependencias de .NET**
    Este comando descarga todos los paquetes NuGet necesarios para la API y las pruebas.
    ```bash
    dotnet restore
    ```

3.  **Instalar los Navegadores para Playwright**
    Este es un paso **crucial**. El siguiente comando ejecuta una rutina especial para descargar los navegadores (Chromium, Firefox, etc.) que Playwright necesita para funcionar.
    ```bash
    dotnet run --project HighRiskEntitySearcherApi install-playwright
    ```

4.  **Habilitar los CORS**
    Si la API se usará desde otra aplicación (como un frontend en un dominio diferente), es crucial habilitar los CORS (Cross-Origin Resource Sharing).Para configurarlo, simplemente modifica la sección "CorsSettings" en tu archivo de configuración (normalmente appsettings.json). En la lista "AllowedOrigins", agrega las URLs de los dominios desde los cuales se conectarán a tu API.
    ```json
    {
        ...,
        "CorsSettings": {
            "AllowedOrigins": [
                "http://localhost:XXXX",
            ]
        }
    }
    ```

5.  **Ejecutar la API**
    Una vez que las dependencias están listas, lanza el servidor de la API.
    ```bash
    dotnet run --project HighRiskEntitySearcherApi
    ```
    La terminal mostrará las URLs en las que está escuchando la API, usualmente `https://localhost:XXXX`.

6.  **Ejecutar las Pruebas (Opcional)**
    Para verificar que todo funciona correctamente, puedes ejecutar el conjunto de pruebas de integración desde la carpeta raíz.
    ```bash
    dotnet test
    ```

---

## Uso de la API y Ejemplos de Solicitudes

La API expone un único endpoint.

-   **Endpoint:** `GET /api/search`
-   **Parámetro:** `entityName` (string, requerido)

### Ejemplos

Puedes usar cURL, Postman o cualquier cliente HTTP para hacer las peticiones.

1.  **Buscar una entidad en la lista de OFAC:**
    ```
    GET https://localhost:XXXX/api/search?entityName=cuba
    ```

2.  **Buscar una entidad en la lista del World Bank:**
    ```
    GET https://localhost:XXXX/api/search?entityName=COESA
    ```

3.  **Buscar una entidad que activa la simulación de Offshore Leaks:**
    ```
    GET https://localhost:XXXX/api/search?entityName=pacific
    ```

4.  **Buscar una entidad que no existe (respuesta vacía):**
    ```
    GET https://localhost:XXXX/api/search?entityName=MyNonExistentTestCompany123
    ```

5.  **Petición incorrecta sin nombre de entidad (devuelve 400 Bad Request):**
    ```
    GET https://localhost:XXXX/api/search?entityName=
    ```

---
## Entregables

-   **Código Fuente:** Disponible en este repositorio.
-   **Colección de Postman:** El archivo `HighRiskEntitySearcher.postman_collection.json` se encuentra en la raíz del repositorio y contiene todos los ejemplos de solicitudes listos para ser importados. Es posible visualizar la colección de la REST API https://documenter.getpostman.com/view/35040380/2sB34Zq4T2

