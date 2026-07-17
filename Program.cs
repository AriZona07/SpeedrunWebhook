using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;

public class Program
{
    // --- CLIENTE HTTP ---
    // Se usa un cliente HttpClient estático para reutilizar conexiones y evitar el agotamiento de puertos.
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task Main(string[] args)
    {
        // Carga las variables de entorno desde un archivo .env en el directorio del proyecto.
        // Esto es ideal para el desarrollo local. En GitHub Actions, las variables se inyectarán directamente.
        Env.Load();

        // --- CONFIGURACIÓN ---
        // Lee las variables desde el entorno.
        var discordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
        var speedrunUsername = Environment.GetEnvironmentVariable("SPEEDRUN_USERNAME");

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(discordWebhookUrl) || string.IsNullOrEmpty(speedrunUsername))
        {
            Console.WriteLine("Error: Las variables de entorno DISCORD_WEBHOOK_URL y SPEEDRUN_USERNAME deben estar configuradas.");
            Console.WriteLine("Crea un archivo .env en la raíz del proyecto o configúralas en tu entorno.");
            return;
        }

        Console.WriteLine($"Buscando récords para el usuario: {speedrunUsername}...");

        try
        {
            // 1. Obtener el ID del usuario de Speedrun.com
            var userId = await GetSpeedrunUserId(speedrunUsername);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"Error: No se pudo encontrar al usuario '{speedrunUsername}'.");
                return;
            }
            Console.WriteLine($"Usuario encontrado con ID: {userId}");

            // 2. Obtener los mejores récords (Personal Bests)
            var personalBests = await GetPersonalBests(userId);
            if (personalBests == null || !personalBests.Any())
            {
                Console.WriteLine("No se encontraron récords para este usuario.");
                return;
            }
            Console.WriteLine($"Se encontraron {personalBests.Count()} récords. Procesando...");

            // 3. Construir el mensaje para Discord
            var discordPayload = BuildDiscordPayload(personalBests, speedrunUsername);

            // 4. Enviar el mensaje al Webhook de Discord
            await SendToDiscord(discordPayload, discordWebhookUrl);

            Console.WriteLine("¡Récords enviados a Discord exitosamente!");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error de red: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");
        }
    }

    private static async Task<string> GetSpeedrunUserId(string username)
    {
        var url = $"https://www.speedrun.com/api/v1/users?lookup={Uri.EscapeDataString(username)}";
        var responseString = await httpClient.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<ApiResponse<List<User>>>(responseString);
        return response?.Data?.FirstOrDefault()?.Id;
    }

    private static async Task<IEnumerable<PersonalBest>> GetPersonalBests(string userId)
    {
        var url = $"https://www.speedrun.com/api/v1/users/{userId}/personal-bests?embed=game,category";
        var responseString = await httpClient.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<ApiResponse<List<PersonalBest>>>(responseString);
        return response?.Data;
    }

    private static DiscordWebhookPayload BuildDiscordPayload(IEnumerable<PersonalBest> personalBests, string username)
    {
        var embeds = new List<DiscordEmbed>();

        foreach (var pb in personalBests.OrderBy(p => p.Place).Take(5))
        {
            var gameName = pb.Game.Data.Names.International;
            var categoryName = pb.Category.Data.Name;
            var runTime = TimeSpan.FromSeconds(pb.Run.Times.PrimaryT);
            var formattedTime = runTime.ToString(@"hh\h\ mm\m\ ss\s");
            
            var embed = new DiscordEmbed
            {
                Title = gameName,
                Color = 16711680,
                Author = new EmbedAuthor
                {
                    Name = $"🏆 Récord de {username}",
                    Url = $"https://www.speedrun.com/user/{username}"
                },
                Fields = new List<EmbedField>
                {
                    new EmbedField { Name = "Categoría", Value = categoryName, Inline = true },
                    new EmbedField { Name = "Tiempo", Value = formattedTime, Inline = true },
                    new EmbedField { Name = "Posición Mundial", Value = $"#{pb.Place}", Inline = true }
                },
                Url = pb.Run.Weblink
            };
            embeds.Add(embed);
        }

        return new DiscordWebhookPayload
        {
            Username = "Speedrun Bot",
            AvatarUrl = "https://www.speedrun.com/images/1st.png",
            Embeds = embeds
        };
    }

    private static async Task SendToDiscord(DiscordWebhookPayload payload, string webhookUrl)
    {
        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { IgnoreNullValues = true });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(webhookUrl, content);
        response.EnsureSuccessStatusCode();
    }
}

// --- MODELOS DE DATOS (sin cambios) ---

public class ApiResponse<T> { [JsonPropertyName("data")] public T Data { get; set; } }
public class User { [JsonPropertyName("id")] public string Id { get; set; } }
public class PersonalBest { [JsonPropertyName("place")] public int Place { get; set; } [JsonPropertyName("run")] public Run Run { get; set; } [JsonPropertyName("game")] public ApiLink<GameData> Game { get; set; } [JsonPropertyName("category")] public ApiLink<CategoryData> Category { get; set; } }
public class Run { [JsonPropertyName("times")] public Times Times { get; set; } [JsonPropertyName("weblink")] public string Weblink { get; set; } }
public class Times { [JsonPropertyName("primary_t")] public double PrimaryT { get; set; } }
public class ApiLink<T> { [JsonPropertyName("data")] public T Data { get; set; } }
public class GameData { [JsonPropertyName("names")] public Names Names { get; set; } }
public class Names { [JsonPropertyName("international")] public string International { get; set; } }
public class CategoryData { [JsonPropertyName("name")] public string Name { get; set; } }
public class DiscordWebhookPayload { [JsonPropertyName("username")] public string Username { get; set; } [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; } [JsonPropertyName("embeds")] public List<DiscordEmbed> Embeds { get; set; } }
public class DiscordEmbed { [JsonPropertyName("title")] public string Title { get; set; } [JsonPropertyName("url")] public string Url { get; set; } [JsonPropertyName("color")] public int Color { get; set; } [JsonPropertyName("author")] public EmbedAuthor Author { get; set; } [JsonPropertyName("fields")] public List<EmbedField> Fields { get; set; } }
public class EmbedAuthor { [JsonPropertyName("name")] public string Name { get; set; } [JsonPropertyName("url")] public string Url { get; set; } }
public class EmbedField { [JsonPropertyName("name")] public string Name { get; set; } [JsonPropertyName("value")] public string Value { get; set; } [JsonPropertyName("inline")] public bool Inline { get; set; } }
