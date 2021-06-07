using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureConverterWPF
{
    class Convert
    {
        public static async Task PrepareData(string image, bool folder, bool subfolder, string format) //format 0 = png, 1 = jpg
        {
            if (folder)
            {
                foreach (string imageFileName in Directory.GetFiles(image))
                {
                    byte[] imgData = File.ReadAllBytes(imageFileName);

                    var currentImageFormat = GetImageFormat(imgData);

                    if (currentImageFormat == ImageFormat2.webp)
                    {
                        await ConvertImage(imageFileName, format, true);
                    }
                    else
                    {
                        await ConvertImage(imageFileName, format, false);
                    }

                }

                if (subfolder)
                {
                    foreach (string d in Directory.GetDirectories(image))
                    {
                        foreach (string imageFileName in Directory.GetFiles(d))
                        {
                            byte[] imgData = File.ReadAllBytes(imageFileName);

                            var currentImageFormat = GetImageFormat(imgData);

                            if (currentImageFormat == ImageFormat2.webp)
                            {
                                await ConvertImage(imageFileName, format, true);
                            }
                            else
                            {
                                await ConvertImage(imageFileName, format, false);
                            }
                        }
                    }
                }
            }
            else
            {
                if (File.Exists(image))
                {

                    byte[] imgData = File.ReadAllBytes(image);

                    var currentImageFormat = GetImageFormat(imgData);

                    if (currentImageFormat == ImageFormat2.webp)
                    {
                        await ConvertImage(image, format, true);
                    }
                    else
                    {
                        await ConvertImage(image, format, false);
                    }
                }
            }
        }
        private static async Task ConvertImage(string image, string format, bool webp)
        {
            try
            {
                if (webp)
                {
                    ConvertWebp(image, format);
                }

                else
                {
                    byte[] imgData = File.ReadAllBytes(image);
                    var currentImageFormat = GetImageFormat(imgData);
                    var imageName = Path.GetFileNameWithoutExtension(image);
                    var imagePath = Path.GetDirectoryName(image) + "\\";

                    if (currentImageFormat == ImageFormat2.unknown)
                    {
                        Logger.Log.Ging($"File: {image} is no Image File");
                    }

                    if (currentImageFormat.ToString() == format)
                    {
                        File.Move(image, imagePath + imageName + "." + format);
                        Logger.Log.Ging($"File: {image} is already the Format {format}");
                    }

                    else
                    {
                        Bitmap bitMapImage = new(image);

                        ImageFormat newImageFormat = ImageFormat.Png;

                        if (format == "jpg")
                        {
                            newImageFormat = ImageFormat.Jpeg;
                        }

                        bitMapImage.Save(imagePath + imageName + "." + format, newImageFormat);

                        if (File.Exists(imagePath + imageName + "." + format))
                        {
                            bitMapImage.Dispose();
                            File.Delete(image);
                            Logger.Log.Ging($"File: {image} was Converted");
                        }
                    }
                    
                }
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                Logger.Log.Ging("Error while Converting Image " + ex);
            }
        }

        private static void ConvertWebp(string image, string format)
        {
            try
            {
                var imageName = Path.GetFileNameWithoutExtension(image);
                var imagePath = Path.GetDirectoryName(image) + "\\";
                var outputName = imagePath + imageName + ".png";

                Process dwebp = new Process();
                dwebp.StartInfo.FileName = @".\dwebp.exe";
                dwebp.StartInfo.Arguments = $"\"{image}\" -o \"{outputName}\"";
                dwebp.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                dwebp.StartInfo.CreateNoWindow = true;
                dwebp.EnableRaisingEvents = true;
                dwebp.Exited += (s, e) =>
                {
                    File.Delete(image);
                    Logger.Log.Ging($"File: {image} was Converted");
                };

                _ = dwebp.Start();

                dwebp.WaitForExit();

                if (format == "jpg")
                {
                    using Image bitMapImage = new Bitmap(outputName);
                    bitMapImage.Save(imagePath + imageName + "." + format, ImageFormat.Jpeg);

                    if (File.Exists(imagePath + imageName + "." + format))
                    {
                        bitMapImage.Dispose();
                        File.Delete(outputName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Ging("Error while Converting Webp " + ex);
            }

        }

        private enum ImageFormat2
        {
            jpeg,
            png,
            webp,
            unknown
        }

        private static ImageFormat2 GetImageFormat(byte[] bytes)
        {
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            var webp = new byte[] { 82, 73, 70, 70 }; // webp canon

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat2.png;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat2.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat2.jpeg;

            if (webp.SequenceEqual(bytes.Take(webp.Length)))
                return ImageFormat2.webp;

            return ImageFormat2.unknown;
        }
    }
}
