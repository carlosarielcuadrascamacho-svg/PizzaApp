using System;
using System.Globalization;
using Microsoft.Maui.Graphics;

namespace PizzeriaApp.Converters
{
    public class IndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentIndex && parameter is string targetIndexStr && int.TryParse(targetIndexStr, out int targetIndex))
            {
                // Si el índice actual del pedido es mayor o igual al índice de este estadio, lo pintamos de verde/naranja
                if (currentIndex >= targetIndex)
                {
                    return targetIndex switch
                    {
                        1 => Color.FromArgb("#FF4B3A"), // Recibido
                        2 => Color.FromArgb("#FF9800"), // Cocina
                        3 => Color.FromArgb("#00C853"), // Listo
                        4 => Color.FromArgb("#4CAF50"), // Entregado
                        _ => Color.FromArgb("#FF4B3A")
                    };
                }
            }
            // Si no ha llegado a este paso, lo ponemos en gris claro
            return Color.FromArgb("#F0F0F0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
