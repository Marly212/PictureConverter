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
        public static async Task ConvertImage(string fullPathToImage, string format)
        {
            try
            {
                byte[] imgData = File.ReadAllBytes(fullPathToImage);
                ImageFormat2 currentImageFormat = GetImageFormat(imgData);

                if (currentImageFormat == ImageFormat2.webp)
                {
                    ConvertWebp(fullPathToImage, format);
                }

                else if(currentImageFormat == ImageFormat2.avif)
                {
                    ConvertAvif(fullPathToImage, format);
                }

                else
                {
                    var imageName = Path.GetFileNameWithoutExtension(fullPathToImage);
                    var imageDirectory = Path.GetDirectoryName(fullPathToImage) + "\\";
                    var outputFileFullPath = imageDirectory + imageName + "." + format;

                    int caseForFile = GetCaseForImage(currentImageFormat, format, fullPathToImage);
                    bool pass = false;
                    bool formatIsExtension = false;

                    switch (caseForFile)
                    {
                        case 1:
                            Logger.Log.Ging($"File: {imageName} is no Image File");
                            pass = true;
                            break;

                        case 2:
                            File.Move(fullPathToImage, outputFileFullPath);
                            Logger.Log.Ging($"File: {imageName} is already the Format {format}");
                            break;

                        case 3:
                            pass = true;
                            formatIsExtension = true;
                            break;

                        default:
                            pass = true;
                            break;
                    }

                    if (pass)
                    {
                        if (formatIsExtension)
                        {
                            var newFullPathToImage = imageDirectory + imageName + "2" + "." + format;
                            outputFileFullPath = fullPathToImage;

                            File.Move(fullPathToImage, newFullPathToImage);
                            fullPathToImage = newFullPathToImage;
                        }

                        ImageFormat newImageFormat = ImageFormat.Png;

                        if (format == "jpg")
                        {
                            newImageFormat = ImageFormat.Jpeg;
                        }

                        if (File.Exists(imageDirectory + imageName + "." + format))
                        {
                            File.Move(imageDirectory + imageName + "." + format, imageDirectory + imageName + "_2" + "." + format);
                            fullPathToImage = imageDirectory + imageName + "_2" + "." + format;
                        }

                        Bitmap bitMapImage = new(fullPathToImage);

                        bitMapImage.Save(imageDirectory + imageName + "." + format, newImageFormat);

                        if (File.Exists(fullPathToImage))
                        {
                            bitMapImage.Dispose();
                            File.Delete(fullPathToImage);
                            Logger.Log.Ging($"File: {imageName}.{format} was Converted");
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
        private static void ConvertWebp(string fullPathToImage, string format)
        {
            try
            {
                var imageName = Path.GetFileNameWithoutExtension(fullPathToImage);
                var imagePath = Path.GetDirectoryName(fullPathToImage) + "\\";
                var outputName = imagePath + imageName + ".png";

                Process dwebp = new Process();
                dwebp.StartInfo.FileName = @"Dependency\dwebp.exe";
                dwebp.StartInfo.Arguments = $"\"{fullPathToImage}\" -o \"{outputName}\"";
                dwebp.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                dwebp.StartInfo.CreateNoWindow = true;
                dwebp.EnableRaisingEvents = true;
                dwebp.Exited += (s, e) =>
                {
                    File.Delete(fullPathToImage);
                    Logger.Log.Ging($"File: {imageName} was Converted");
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

        private static void ConvertAvif(string fullPathToImage, string format)
        {
            try
            {
                var imageName = Path.GetFileNameWithoutExtension(fullPathToImage);
                var imagePath = Path.GetDirectoryName(fullPathToImage) + "\\";
                var imageExtension = Path.GetExtension(fullPathToImage);
                var realOutputName = imagePath + imageName + "." + format;

                var tempNewName = "process";
                var tempOutputName = imagePath + tempNewName;

                File.Move(fullPathToImage, tempOutputName + imageExtension);

                Process avif = new();
                avif.StartInfo.FileName = @"Dependency\avifdec.exe";
                avif.StartInfo.Arguments = $"\"{tempOutputName + imageExtension}\" \"{tempOutputName + "." + format}\"";
                avif.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                avif.StartInfo.CreateNoWindow = true;
                avif.EnableRaisingEvents = true;
                avif.Exited += (s, e) =>
                {
                    File.Move(tempOutputName + "." + format, realOutputName);
                    File.Delete(tempOutputName);
                    File.Delete(tempOutputName + imageExtension);
                    Logger.Log.Ging($"File: {imageName} was Converted");
                };

                _ = avif.Start();

                avif.WaitForExit();
            }
            catch (Exception ex)
            {
                Logger.Log.Ging("Error while Converting Avif " + ex);
            }
        }

        private static int GetCaseForImage(ImageFormat2 currentImageFormat, string format, string pathToImage)
        {
            var extension = Path.GetExtension(pathToImage);

            if (currentImageFormat == ImageFormat2.unknown)
            {
                return 1;
            }

            if (currentImageFormat.ToString() == format)
            {
                return 2;
            }

            if (extension == "." + format)
            {
                return 3;
            }
            else
            {
                return 0;
            }
        }

        public enum ImageFormat2
        {
            jpeg,
            png,
            webp,
            avif,
            unknown
        }

        public static ImageFormat2 GetImageFormat(byte[] bytes)
        {
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            var webp = new byte[] { 82, 73, 70, 70 }; // webp canon
            var avif = new byte[] { 0, 0, 0, 28 }; // avif canon

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat2.png;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat2.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat2.jpeg;

            if (webp.SequenceEqual(bytes.Take(webp.Length)))
                return ImageFormat2.webp;

            if (avif.SequenceEqual(bytes.Take(avif.Length)))
                return ImageFormat2.avif;

            return ImageFormat2.unknown;
        }
    }
}
