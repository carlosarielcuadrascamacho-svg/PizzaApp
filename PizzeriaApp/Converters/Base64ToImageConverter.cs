using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace PizzeriaApp.Converters
{
    // Este convertidor es clave para mostrar las fotos de las pizzas o del perfil del cliente que vienen como string desde la base de datos
    public class Base64ToImageConverter : IValueConverter
    {
        // El método Convert se encarga de transformar el texto Base64 que viene de Supabase en algo que la UI de MAUI pueda pintar
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Primero checamos que lo que nos llega sea un string y que no esté vacío, para no tronar la app con nulos
            if (value is string base64String && !string.IsNullOrWhiteSpace(base64String))
            {
                try
                {
                    // A veces las imágenes vienen con un prefijo (ej. data:image/png;base64,), aquí lo limpiamos para quedarnos puro con la data
                    if (base64String.Contains(","))
                    {
                        // Cortamos el string justo después de la coma para obtener el Base64 limpio
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }

                    // Convertimos esa cadena de texto en un arreglo de bytes, que es lo que realmente representa la imagen
                    byte[] imageBytes = System.Convert.FromBase64String(base64String);
                    
                    // Retornamos el ImageSource usando un stream de memoria; esto es súper eficiente para cargar fotos sin guardarlas en disco
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch (FormatException ex)
                {
                    // Si el formato del Base64 está mal (corrupto), lo cachamos aquí para avisar en consola y no cerrar la app
                    Console.WriteLine($"Error convirtiendo Base64: {ex.Message}");
                    return null;
                }

            }

            // Si no hay datos, regresamos nulo para que el control de imagen simplemente no muestre nada
            return null;
        }

        // Este método casi nunca se usa en convertidores de imagen, pero por contrato de interfaz hay que definirlo
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Por ahora no necesitamos convertir de imagen a Base64 al hacer binding hacia atrás, así que lanzamos la excepción
            throw new NotImplementedException("No se soporta conversión bidireccional desde Vista a Base64 nativamente aquí.");
        }
    }
}
