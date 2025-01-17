using Chat.Mvc.Proxies;

namespace Chat.Mvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración del cliente HTTP para interactuar con la API
            builder.Services.AddHttpClient<IChatApiProxy, ChatApiProxy>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7230/api/");
            });

            // Agregar servicios al contenedor
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configuración del pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Mensajes}/{action=Index}/{id?}");
            });

            app.UseAuthorization();

            app.Run();
        }
    }
}
