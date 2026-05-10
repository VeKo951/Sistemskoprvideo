using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Sistemskoprvideo
{
    internal class ImageProcessor
    {
        public byte[] ConvertToGrayscale(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLower();

            using (Bitmap original = new Bitmap(imagePath))
            {
                using (Bitmap grayscale = new Bitmap(original.Width, original.Height))
                {
                    for (int y = 0; y < original.Height; y++)
                    {
                        for (int x = 0; x < original.Width; x++)
                        {
                            Color pixel = original.GetPixel(x, y);

                            int gray = (pixel.R + pixel.G + pixel.B) / 3;

                            Color newColor = Color.FromArgb(gray, gray, gray);

                            grayscale.SetPixel(x, y, newColor);
                        }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (extension == ".png")
                        {
                            grayscale.Save(ms, ImageFormat.Png);
                        }
                        else
                        {
                            grayscale.Save(ms, ImageFormat.Jpeg);
                        }

                        return ms.ToArray();
                    }
                }
            }
        }
    }
}