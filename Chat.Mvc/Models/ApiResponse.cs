namespace Chat.Mvc.Models
{

        public class ApiResponse<T>
        {
            public bool Success { get; set; } // Indica si la operación fue exitosa
            public T Mensajes { get; set; } // Contiene los mensajes devueltos por la API
        }
    }

