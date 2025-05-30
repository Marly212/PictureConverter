using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick; // Added for Magick.NET

namespace PictureConverterWPF
{
    class Convert
    {
        public static async Task ConvertImage(string fullPathToImage, string format)
        {
            try
            {
                var imageName = Path.GetFileNameWithoutExtension(fullPathToImage);
                var imageDirectory = Path.GetDirectoryName(fullPathToImage);
                string desiredOutputFileFullPath = Path.Combine(imageDirectory, imageName + "." + format.ToLowerInvariant());

                if (File.Exists(desiredOutputFileFullPath))
                {
                    string backupPath = Path.Combine(imageDirectory, imageName + "_old_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." + format.ToLowerInvariant());
                    File.Move(desiredOutputFileFullPath, backupPath);
                    Logger.Log.Ging($"Existing file at {desiredOutputFileFullPath} was moved to {backupPath}.");
                }

                byte[] imgData = File.ReadAllBytes(fullPathToImage);
                ImageFormat2 currentImageFormat = GetImageFormat(imgData);

                if (currentImageFormat == ImageFormat2.webp || currentImageFormat == ImageFormat2.avif)
                {
                    using (MagickImage image = new MagickImage(fullPathToImage))
                    {
                        MagickFormat targetMagickFormat = GetMagickFormat(format);
                        if (targetMagickFormat == MagickFormat.Unknown) // Check if GetMagickFormat returned Unknown
                        {
                            Logger.Log.Ging($"Unsupported target format: {format} for Magick.NET conversion. Skipping {Path.GetFileName(fullPathToImage)}.");
                            return; // Exit if format is not supported by this logic
                        }
                        image.Format = targetMagickFormat;
                        image.Write(desiredOutputFileFullPath);
                        File.Delete(fullPathToImage); // Delete original file after successful conversion
                        Logger.Log.Ging($"File: {fullPathToImage} was converted to {desiredOutputFileFullPath} using Magick.NET. Original deleted.");
                    }
                }
                else
                {
                    // var imageName = Path.GetFileNameWithoutExtension(fullPathToImage); // Already defined above
                    // var imageDirectory = Path.GetDirectoryName(fullPathToImage); // Already defined above
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

                    // Proceed with conversion (this part is for non-WebP/AVIF that are not already correct)
                    // string desiredOutputFileFullPath = Path.Combine(imageDirectory, imageName + "." + targetFormatLower); // Already defined

                    // if (File.Exists(desiredOutputFileFullPath)) // Already handled
                    // {
                    // string backupPath = Path.Combine(imageDirectory, imageName + "_old_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." + targetFormatLower);
                    // File.Move(desiredOutputFileFullPath, backupPath);
                    // Logger.Log.Ging($"Existing file at {desiredOutputFileFullPath} was moved to {backupPath}.");
                    // }

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

        private static MagickFormat GetMagickFormat(string format)
        {
            switch (format.ToLowerInvariant())
            {
                case "jpg":
                case "jpeg":
                    return MagickFormat.Jpg;
                case "png":
                    return MagickFormat.Png;
                case "gif":
                    return MagickFormat.Gif;
                case "bmp":
                    return MagickFormat.Bmp;
                case "tiff":
                case "tif":
                    return MagickFormat.Tiff;
                case "webp":
                    return MagickFormat.WebP;
                case "avif":
                    return MagickFormat.Avif;
                // Add other formats as needed
                default:
                    return MagickFormat.Unknown; // Or throw an exception, or return a default
            }
        }
        // Removed ConvertWebp method
        // Removed ConvertAvif method

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
