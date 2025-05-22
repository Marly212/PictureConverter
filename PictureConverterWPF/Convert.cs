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
                    var imageDirectory = Path.GetDirectoryName(fullPathToImage); // Removed trailing "\\" for Path.Combine
                    string originalExtension = Path.GetExtension(fullPathToImage).TrimStart('.').ToLowerInvariant();
                    string targetFormatLower = format.ToLowerInvariant();

                    if (currentImageFormat == ImageFormat2.unknown)
                    {
                        Logger.Log.Ging($"File: {imageName}{Path.GetExtension(fullPathToImage)} is not a recognized image type or is corrupted. Skipping.");
                        return; // Early exit
                    }

                    if (currentImageFormat.ToString().ToLowerInvariant() == targetFormatLower)
                    {
                        if (originalExtension != targetFormatLower)
                        {
                            // Internal format is correct, but extension is wrong. Rename.
                            string newPathWithCorrectExtension = Path.Combine(imageDirectory, imageName + "." + targetFormatLower);
                            if (File.Exists(newPathWithCorrectExtension))
                            {
                                // If a file with the target extension already exists, handle it (e.g., by backing it up or logging a conflict)
                                string backupPathForExisting = Path.Combine(imageDirectory, imageName + "_existing_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." + targetFormatLower);
                                File.Move(newPathWithCorrectExtension, backupPathForExisting);
                                Logger.Log.Ging($"File: {newPathWithCorrectExtension} already existed. Moved to {backupPathForExisting}.");
                            }
                            File.Move(fullPathToImage, newPathWithCorrectExtension);
                            Logger.Log.Ging($"File: {imageName}{Path.GetExtension(fullPathToImage)} was already {targetFormatLower} format but had wrong extension. Renamed to {newPathWithCorrectExtension}.");
                        }
                        else
                        {
                            // Both internal format and extension are correct.
                            Logger.Log.Ging($"File: {fullPathToImage} is already in the correct {targetFormatLower} format. Skipping.");
                        }
                        return; // Early exit, no conversion needed
                    }

                    // Proceed with conversion
                    string desiredOutputFileFullPath = Path.Combine(imageDirectory, imageName + "." + targetFormatLower);

                    if (File.Exists(desiredOutputFileFullPath))
                    {
                        string backupPath = Path.Combine(imageDirectory, imageName + "_old_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." + targetFormatLower);
                        File.Move(desiredOutputFileFullPath, backupPath);
                        Logger.Log.Ging($"Existing file at {desiredOutputFileFullPath} was moved to {backupPath}.");
                    }

                    ImageFormat newImageFormat = ImageFormat.Png; // Default
                    if (targetFormatLower == "jpg" || targetFormatLower == "jpeg")
                    {
                        newImageFormat = ImageFormat.Jpeg;
                    }
                    else if (targetFormatLower == "tif" || targetFormatLower == "tiff")
                    {
                        newImageFormat = ImageFormat.Tiff;
                    }
                    // GIF is handled by System.Drawing.Bitmap.Save with Png ImageFormat, but if specific GIF options were needed, this would be different.
                    // For now, it will be saved as PNG if "gif" is chosen, unless System.Drawing auto-detects.
                    // To save as actual GIF: newImageFormat = ImageFormat.Gif; (but ensure input is suitable or handle System.Exception)

                    using (Bitmap bitMapImage = new Bitmap(fullPathToImage))
                    {
                        bitMapImage.Save(desiredOutputFileFullPath, newImageFormat);
                    }

                    // If Save was successful, delete the original file
                    File.Delete(fullPathToImage);
                    Logger.Log.Ging($"File: {fullPathToImage} was converted to {desiredOutputFileFullPath}. Original deleted.");
                }
                await Task.Delay(10); // This delay seems to be for UI responsiveness or to avoid rapid file operations. Keeping it.
            }
            catch (Exception ex)
            {
                Logger.Log.Ging($"Error while Converting Image {Path.GetFileName(fullPathToImage)}: " + ex.Message);
                // Original file is not deleted in case of error, which is good.
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

        public enum ImageFormat2
        {
            jpeg,
            png,
            gif,
            webp,
            avif,
            tiff,
            unknown
        }

        public static ImageFormat2 GetImageFormat(byte[] bytes)
        {
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            var gif = new byte[] { 71, 73, 70, 56 }; // gif canon
            var webp = new byte[] { 82, 73, 70, 70 }; // webp canon
            var avif = new byte[] { 0, 0, 0, 28 }; // avif canon
            var tiff_ii = new byte[] { 0x49, 0x49, 0x2A, 0x00 }; // TIFF II*
            var tiff_mm = new byte[] { 0x4D, 0x4D, 0x00, 0x2A }; // TIFF MM*

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat2.png;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat2.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat2.jpeg;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat2.gif;

            if (webp.SequenceEqual(bytes.Take(webp.Length)))
                return ImageFormat2.webp;

            if (avif.SequenceEqual(bytes.Take(avif.Length)))
                return ImageFormat2.avif;

            if (tiff_ii.SequenceEqual(bytes.Take(tiff_ii.Length)))
                return ImageFormat2.tiff;

            if (tiff_mm.SequenceEqual(bytes.Take(tiff_mm.Length)))
                return ImageFormat2.tiff;

            return ImageFormat2.unknown;
        }
    }
}
