using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace PizzeriaApp.Converters
{
    public class Base64ToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string base64String && !string.IsNullOrWhiteSpace(base64String))
            {
                try
                {
                    if (base64String.Contains(","))
                    {
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }

                    byte[] imageBytes = System.Convert.FromBase64String(base64String);
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Error convirtiendo Base64: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("No se soporta conversión bidireccional desde Vista a Base64 nativamente aquí.");
        }
    }
}
