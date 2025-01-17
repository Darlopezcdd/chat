using Chat.Mvc.Proxies;
using chat.Modelos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Chat.Mvc.Models;

namespace Chat.Mvc.Controllers
{
    public class MensajesController : Controller
    {
        private readonly IChatApiProxy _chatApiProxy;

        public MensajesController(IChatApiProxy chatApiProxy)
        {
            _chatApiProxy = chatApiProxy;
        }

        // Mostrar mensajes enviados y recibidos por el usuario actual
        public async Task<IActionResult> Index(int? userId, int? grupoId)
        {
            var chatViewModel = new ChatViewModel();

            try
            {
                // Obtener lista de usuarios
                var usuarios = await _chatApiProxy.GetUsersAsync();

                if (grupoId.HasValue)
                {
                    await _chatApiProxy.MarcarMensajesGrupoComoLeidosAsync(grupoId.Value);
                    var mensajes = await _chatApiProxy.GetMensajesAsync();

                    // Transformar mensajes a MensajeConNombres
                    chatViewModel.Mensajes = mensajes
                        .Where(m => m.GrupoId == grupoId.Value)
                        .Select(m => new MensajeConNombres
                        {
                            Id = m.Id,
                            Contenido = m.Contenido,
                            FechaEnvio = m.FechaEnvio,
                            NombreRemitente = usuarios.FirstOrDefault(u => u.Id == m.UserRemitenteId)?.Name ?? "Desconocido",
                            NombreDestinatario = "Grupo"
                        })
                        .ToList();
                }
                else if (userId.HasValue)
                {
                    await _chatApiProxy.MarcarMensajesComoLeidosAsync(userId.Value);
                    var mensajes = await _chatApiProxy.GetMensajesAsync();

                    // Transformar mensajes a MensajeConNombres
                    chatViewModel.Mensajes = mensajes
                        .Where(m => m.UserRemitenteId == userId.Value || m.UserDestinatarioId == userId.Value)
                        .Select(m => new MensajeConNombres
                        {
                            Id = m.Id,
                            Contenido = m.Contenido,
                            FechaEnvio = m.FechaEnvio,
                            NombreRemitente = usuarios.FirstOrDefault(u => u.Id == m.UserRemitenteId)?.Name ?? "Desconocido",
                            NombreDestinatario = usuarios.FirstOrDefault(u => u.Id == m.UserDestinatarioId)?.Name ?? "Desconocido"
                        })
                        .ToList();
                }

                // Manejo de grupos
                var grupos = await _chatApiProxy.GetGruposAsync();
                if (grupos == null || grupos.Count == 0)
                {
                    ViewBag.MensajeGrupos = "No hay grupos disponibles en este momento.";
                    chatViewModel.Grupos = new List<Grupo>();
                }
                else
                {
                    chatViewModel.Grupos = grupos;
                }

                // Agregar usuarios al modelo
                chatViewModel.Usuarios = usuarios;
                chatViewModel.UsuarioActivoId = userId ?? 1; // Usuario predeterminado o según lógica
            }
            catch (Exception ex)
            {
                // Manejo de errores generales
                Console.WriteLine($"Error al cargar la página: {ex.Message}");
                ViewBag.MensajeError = "Ocurrió un error al cargar los datos. Inténtelo nuevamente más tarde.";
            }

            return View(chatViewModel);
        }


        // Mostrar formulario para enviar un mensaje
        public IActionResult Create(int remitenteId)
        {
            var mensaje = new Mensaje
            {
                UserRemitenteId = remitenteId
            };

            return View(mensaje);
        }


        // Enviar un mensaje
        [HttpPost]
        public async Task<IActionResult> Create(Mensaje mensaje)
        {
            if (ModelState.IsValid)
            {
                mensaje.FechaEnvio = DateTime.Now;

                // Log para depuración
                Console.WriteLine($"Enviando mensaje desde el controlador: {JsonConvert.SerializeObject(mensaje)}");

                var success = await _chatApiProxy.CreateMensajeAsync(mensaje);
                if (success)
                {
                    return RedirectToAction("Index", new { userId = mensaje.UserRemitenteId });
                }
                ModelState.AddModelError("", "Error al enviar el mensaje a través del proxy.");
            }

            return View(mensaje);
        }
    }
}
