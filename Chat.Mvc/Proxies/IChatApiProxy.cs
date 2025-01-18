using chat.Modelos;

namespace Chat.Mvc.Proxies
{
    public interface IChatApiProxy
    {
        Task<List<User>> GetUsersAsync();
        Task<User> GetUserByIdAsync(int id);
        Task<bool> CreateUserAsync(User user);

        Task<List<Mensaje>> GetMensajesAsync();
		Task<bool> CreateMensajeAsync(Mensaje mensaje);
        Task<bool> MarcarMensajesComoLeidosAsync(List<Mensaje> mensajes);
        Task<List<Grupo>> GetGruposAsync();
        Task<bool> MarcarMensajesGrupoComoLeidosAsync(int grupoId);
        Task<bool> DeleteUserAsync(int id);

    }
}
