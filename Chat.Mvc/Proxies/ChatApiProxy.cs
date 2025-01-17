using chat.Modelos;
using Chat.Mvc.Proxies;
using Newtonsoft.Json;
using System.Text;

public class ChatApiProxy : IChatApiProxy
{
    private readonly HttpClient _httpClient;

    public ChatApiProxy(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("Users");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<User>>(content);
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"Users/{id}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<User>(content);
    }

    //public async Task<bool> CreateUserAsync(User user)
    //{
    //    var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
    //    var response = await _httpClient.PostAsync("Users", content);

    //    return response.IsSuccessStatusCode;
    //}

    public async Task<List<Mensaje>> GetMensajesAsync()
    {
        var response = await _httpClient.GetAsync("Mensajes");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Mensaje>>(content);
    }

    public async Task<bool> CreateMensajeAsync(Mensaje mensaje)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(mensaje), Encoding.UTF8, "application/json");
            Console.WriteLine($"JSON enviado al proxy: {JsonConvert.SerializeObject(mensaje)}");

            var response = await _httpClient.PostAsync("Mensajes", content);

            Console.WriteLine($"Response Status Code: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en el proxy: {ex.Message}");
            return false;
        }
    }
    public async Task<bool> CheckNewMessagesAsync()
    {
        var response = await _httpClient.GetAsync("Mensajes/NuevosMensajes");
        response.EnsureSuccessStatusCode();
        return bool.Parse(await response.Content.ReadAsStringAsync());
    }
    public async Task<bool> MarcarMensajesComoLeidosAsync(int userId)
    {
        var response = await _httpClient.PutAsync($"Mensajes/MarcarComoLeidos/{userId}", null);
        return response.IsSuccessStatusCode;
    }
    public async Task<List<Grupo>> GetGruposAsync()
    {
        try
        {
            // Realiza la solicitud al endpoint
            var response = await _httpClient.GetAsync("Grupos");
            response.EnsureSuccessStatusCode();

            // Procesa la respuesta si es exitosa
            var content = await response.Content.ReadAsStringAsync();
            var grupos = JsonConvert.DeserializeObject<List<Grupo>>(content);

            // Si la lista de grupos está vacía, informa
            if (grupos == null || grupos.Count == 0)
            {
                Console.WriteLine("No se encontraron grupos disponibles.");
                return new List<Grupo>(); // Devuelve lista vacía
            }

            return grupos;
        }
        catch (HttpRequestException ex)
        {
            // Manejo de errores HTTP (por ejemplo, 404, 500)
            Console.WriteLine($"Error al obtener grupos: {ex.Message}");
        }
        catch (JsonException ex)
        {
            // Manejo de errores de deserialización
            Console.WriteLine($"Error al procesar la respuesta de la API: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Otros errores inesperados
            Console.WriteLine($"Error inesperado: {ex.Message}");
        }

        // Devuelve una lista vacía si ocurre un error y registra un mensaje
        Console.WriteLine("No se pudieron cargar los grupos debido a un error.");
        return new List<Grupo>();
    }

    public async Task<bool> MarcarMensajesGrupoComoLeidosAsync(int grupoId)
    {
        var response = await _httpClient.PutAsync($"Mensajes/MarcarMensajesGrupoComoLeidos/{grupoId}", null);
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("Users", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error al crear usuario: {response.StatusCode} - {response.ReasonPhrase}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreateUserAsync: {ex.Message}");
            return false;
        }
    }
    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"Users/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en DeleteUserAsync: {ex.Message}");
            return false;
        }
    }



}




