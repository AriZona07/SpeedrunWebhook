# Speedrun.com Discord Webhook

Este proyecto es una aplicación de consola en C# que obtiene los mejores récords personales (Personal Bests o PBs) de un usuario de Speedrun.com y los publica en un canal de Discord a través de un Webhook.

La aplicación está diseñada para ser ejecutada tanto localmente para pruebas como de forma automatizada usando GitHub Actions.

## Características

- **Consulta a la API de Speedrun.com**: Obtiene los PBs de un usuario específico.
- **Extracción de Datos**: Extrae el nombre del juego, la categoría y el tiempo del récord.
- **Integración con Discord**: Construye y envía un mensaje formateado a un Webhook de Discord usando Embeds para una mejor visualización.
- **Seguridad**: Maneja las claves secretas (Webhook URL) y la configuración (nombre de usuario) a través de variables de entorno, evitando exponerlas en el código fuente.
- **Automatización**: Incluye un flujo de trabajo de GitHub Actions para ejecutar la aplicación de forma programada.

## Cómo Empezar

### Prerrequisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (versión 6.0 o superior)
- Una cuenta de GitHub para la automatización.
- Un Webhook de Discord.

---

### 1. Configuración para Desarrollo Local

Para probar la aplicación en tu máquina local:

1.  **Clona el repositorio.**

2.  **Crea un archivo `.env`**:
    Copia el archivo `.env.example` y renómbralo a `.env`.

    ```bash
    cp .env.example .env
    ```

3.  **Modifica el archivo `.env`**:
    Abre el archivo `.env` y reemplaza los valores de ejemplo con tus datos reales.

    ```dotenv
    # La URL completa del Webhook de tu canal de Discord.
    DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/..."

    # El nombre de usuario exacto de Speedrun.com que quieres consultar.
    SPEEDRUN_USERNAME="nombredeusuario"
    ```
    El archivo `.gitignore` ya está configurado para que `.env` no se suba a tu repositorio.

4.  **Ejecuta la aplicación**:
    Abre una terminal en la raíz del proyecto y ejecuta los siguientes comandos:

    ```bash
    # Restaura las dependencias (solo la primera vez)
    dotnet restore

    # Ejecuta la aplicación
    dotnet run
    ```

---

### 2. Configuración para Automatización con GitHub Actions

Para que la aplicación se ejecute automáticamente en un horario definido (por ejemplo, cada 6 horas):

1.  **Sube el código a tu repositorio de GitHub.**

2.  **Configura los Secrets en GitHub**:
    Los secretos (secrets) son variables de entorno cifradas que solo están disponibles para los flujos de trabajo de GitHub Actions.

    - Ve a tu repositorio en GitHub.
    - Haz clic en **Settings** > **Secrets and variables** > **Actions**.
    - Haz clic en **New repository secret** para añadir cada una de las siguientes variables:

      - **`DISCORD_WEBHOOK_URL`**: Pega aquí la URL de tu Webhook de Discord.
      - **`SPEEDRUN_USERNAME`**: Escribe el nombre de usuario de Speedrun.com.

    ![GitHub Secrets](httpsf://docs.github.com/assets/cb-123734/images/help/settings/actions-secrets-new-secret-button.png)

3.  **Activa el flujo de trabajo**:
    El flujo de trabajo definido en `.github/workflows/main.yml` se activará automáticamente según el `cron` especificado (por defecto, cada 24 horas).

    También puedes ejecutarlo manualmente:
    - Ve a la pestaña **Actions** en tu repositorio.
    - Selecciona el flujo de trabajo **Check and Post Speedrun PBs**.
    - Haz clic en **Run workflow**.

## Estructura del Código

- **`Program.cs`**: Lógica principal de la aplicación. Lee las variables de entorno, consulta las APIs y envía el mensaje.
- **`SpeedrunWebhook.csproj`**: Archivo de proyecto que define las dependencias, como `DotNetEnv` para cargar el archivo `.env` en desarrollo local.
- **`.env.example`**: Plantilla para el archivo de configuración local.
- **`.gitignore`**: Asegura que los archivos sensibles como `.env` no se incluyan en el control de versiones.
- **`.github/workflows/main.yml`**: Define el flujo de trabajo de GitHub Actions para la ejecución automatizada.
