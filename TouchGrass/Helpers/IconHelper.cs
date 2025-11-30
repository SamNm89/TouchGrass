using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TouchGrass.Helpers
{
    public static class IconHelper
    {
        public static string? ExtractIconToPng(string filePath, string outputFolder)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                // Extract the icon associated with the file
                using (Icon? icon = Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon == null) return null;

                    // Create output folder if it doesn't exist
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    // Generate unique filename
                    string uniqueName = $"{Guid.NewGuid()}.png";
                    string outputPath = Path.Combine(outputFolder, uniqueName);

                    // Convert Icon to Bitmap and save as PNG
                    using (Bitmap bitmap = icon.ToBitmap())
                    {
                        bitmap.Save(outputPath, ImageFormat.Png);
                    }

                    return outputPath;
                }
            }
            catch (Exception)
            {
                // Log error if needed
                return null;
            }
        }
    }
}
