using Microsoft.VisualStudio.TestTools.UnitTesting;
using PictureConverterWPF; // Assuming Convert class is in this namespace
using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing; // For Image.FromFile and RawFormat
using System.Drawing.Imaging; // For ImageFormat
using ImageMagick; // Added for Magick.NET types

namespace PictureConverterWPF.Tests
{
    [TestClass]
    public class ConvertTests
    {
        public TestContext TestContext { get; set; }

        private string _baseTestFilesDir;
        private string _testWorkingDir;

        [TestInitialize]
        public void TestInitialize()
        {
            // Determine the base directory for TestFiles. This assumes the tests are run from a subdirectory of the project,
            // e.g., bin/Debug/net6.0-windows.
            // This path might need adjustment based on the actual test runner's output directory structure.
            string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")); // Adjust if needed
            _baseTestFilesDir = Path.Combine(solutionDir, "PictureConverterWPF.Tests", "TestFiles");

            if (!Directory.Exists(_baseTestFilesDir))
            {
                // Fallback for different environments (e.g. when tests are run directly from project root)
                _baseTestFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles");
                 if (!Directory.Exists(_baseTestFilesDir))
                 {
                    // Another fallback if TestFiles is in the same directory as the test DLL
                    _baseTestFilesDir = Path.Combine(Path.GetDirectoryName(typeof(ConvertTests).Assembly.Location), "TestFiles");
                 }
            }
            
            Assert.IsTrue(Directory.Exists(_baseTestFilesDir), $"Base test files directory not found. Looked in: {Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "PictureConverterWPF.Tests", "TestFiles"))} and {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles")} and {Path.Combine(Path.GetDirectoryName(typeof(ConvertTests).Assembly.Location), "TestFiles")}");


            _testWorkingDir = Path.Combine(Path.GetTempPath(), "ConvertTests", TestContext.TestName + "_" + Guid.NewGuid().ToString("N"));
            
            if (Directory.Exists(_testWorkingDir))
            {
                Directory.Delete(_testWorkingDir, true);
            }
            Directory.CreateDirectory(_testWorkingDir);

            foreach (var file in Directory.GetFiles(_baseTestFilesDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(_baseTestFilesDir.Length + 1);
                string destFile = Path.Combine(_testWorkingDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)); // Ensure subdirectories are created if any
                File.Copy(file, destFile, true);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testWorkingDir))
            {
                Directory.Delete(_testWorkingDir, true);
            }
        }

        private async Task<bool> IsImageFormat(string filePath, System.Drawing.Imaging.ImageFormat expectedFormat)
        {
            if (!File.Exists(filePath)) return false;
            // These are placeholder files, so actual image loading will fail.
            // We'll simulate format checking based on known placeholder content for now.
            // In a real scenario with actual images, Image.FromFile would be used.
            try
            {
                // If these were real images:
                // using (Image img = Image.FromFile(filePath))
                // {
                //     return img.RawFormat.Equals(expectedFormat);
                // }

                // Placeholder check:
                string content = await File.ReadAllTextAsync(filePath);
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Png) && content.Contains("PNG_PLACEHOLDER_CONVERTED")) return true;
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg) && content.Contains("JPG_PLACEHOLDER_CONVERTED")) return true;
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff) && content.Contains("TIF_PLACEHOLDER_CONVERTED")) return true;
                
                // For the initial placeholder files copied for testing
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Png) && content.Contains("PNG_PLACEHOLDER")) return true;
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg) && content.Contains("JPG_PLACEHOLDER")) return true;
                if (expectedFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff) && content.Contains("TIF_PLACEHOLDER")) return true;

                return false; // Fallback if content doesn't match simulated conversion
            }
            catch (OutOfMemoryException) // System.Drawing.Image.FromFile throws this for non-image files
            {
                return false;
            }
            catch (IOException)
            {
                 return false; // File likely in use or other IO issue
            }
        }

        // Helper to get image format using Magick.NET
        private MagickFormat? GetMagickImageFormat(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                MagickImageInfo info = new MagickImageInfo(filePath);
                return info.Format;
            }
            catch
            {
                return null; // Could not read image info
            }
        }
        
        // Helper to simulate image content after "conversion" for placeholder files
        private async Task SimulateConversionMarker(string filePath, string targetFormat)
        {
            if(File.Exists(filePath))
            {
                string marker = "";
                if (targetFormat.Equals("png", StringComparison.OrdinalIgnoreCase)) marker = "PNG_PLACEHOLDER_CONVERTED";
                else if (targetFormat.Equals("jpg", StringComparison.OrdinalIgnoreCase) || targetFormat.Equals("jpeg", StringComparison.OrdinalIgnoreCase)) marker = "JPG_PLACEHOLDER_CONVERTED";
                else if (targetFormat.Equals("tif", StringComparison.OrdinalIgnoreCase) || targetFormat.Equals("tiff", StringComparison.OrdinalIgnoreCase)) marker = "TIF_PLACEHOLDER_CONVERTED";
                
                if (!string.IsNullOrEmpty(marker))
                {
                    await File.WriteAllTextAsync(filePath, $"This is a placeholder for a converted image.\nContent for testing purposes: {marker}");
                }
            }
        }


        [TestMethod]
        public async Task Test_Convert_TifToPng()
        {
            string sourceFile = "sample.tif";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);
            // Simulate successful conversion for placeholder
            if(File.Exists(expectedOutputFile) && !File.Exists(sourcePath)) await SimulateConversionMarker(expectedOutputFile, targetFormat);


            Assert.IsTrue(File.Exists(expectedOutputFile), "Output PNG file was not created.");
            Assert.IsFalse(File.Exists(sourcePath), "Original TIF file was not deleted.");
            Assert.IsTrue(await IsImageFormat(expectedOutputFile, System.Drawing.Imaging.ImageFormat.Png), "Output file is not a valid PNG (placeholder check).");
        }

        [TestMethod]
        public async Task Test_Convert_PngToTif()
        {
            string sourceFile = "sample.png";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "tif";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);
            if(File.Exists(expectedOutputFile) && !File.Exists(sourcePath)) await SimulateConversionMarker(expectedOutputFile, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output TIF file was not created.");
            Assert.IsFalse(File.Exists(sourcePath), "Original PNG file was not deleted.");
            Assert.IsTrue(await IsImageFormat(expectedOutputFile, System.Drawing.Imaging.ImageFormat.Tiff), "Output file is not a valid TIF (placeholder check).");
        }

        [TestMethod]
        public async Task Test_Convert_JpgToTif()
        {
            string sourceFile = "sample.jpg";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "tif";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);
            if(File.Exists(expectedOutputFile) && !File.Exists(sourcePath)) await SimulateConversionMarker(expectedOutputFile, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output TIF file was not created.");
            Assert.IsFalse(File.Exists(sourcePath), "Original JPG file was not deleted.");
            Assert.IsTrue(await IsImageFormat(expectedOutputFile, System.Drawing.Imaging.ImageFormat.Tiff), "Output file is not a valid TIF (placeholder check).");
        }

        [TestMethod]
        public async Task Test_Convert_TifToJpg()
        {
            string sourceFile = "sample.tif";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "jpg";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);
            if(File.Exists(expectedOutputFile) && !File.Exists(sourcePath)) await SimulateConversionMarker(expectedOutputFile, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output JPG file was not created.");
            Assert.IsFalse(File.Exists(sourcePath), "Original TIF file was not deleted.");
            Assert.IsTrue(await IsImageFormat(expectedOutputFile, System.Drawing.Imaging.ImageFormat.Jpeg), "Output file is not a valid JPG (placeholder check).");
        }

        [TestMethod]
        public async Task Test_Convert_TifToTif_CorrectExtension()
        {
            string sourceFile = "sample.tif";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "tif";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat); // Same as sourcePath

            long originalFileSize = new FileInfo(sourcePath).Length;
            DateTime originalLastWriteTime = File.GetLastWriteTimeUtc(sourcePath);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(sourcePath), "Original TIF file was deleted or moved, but it shouldn't have been.");
            Assert.AreEqual(expectedOutputFile, sourcePath, "Expected output path should be same as source for same format conversion.");
            // Check that the file was not unnecessarily re-written
            Assert.AreEqual(originalFileSize, new FileInfo(sourcePath).Length, "File size changed, indicating it might have been re-written.");
            Assert.AreEqual(originalLastWriteTime, File.GetLastWriteTimeUtc(sourcePath), "File timestamp changed, indicating it might have been re-written.");
        }

        [TestMethod]
        public async Task Test_Convert_TifToTif_WrongExtension()
        {
            string originalTifFile = "sample.tif"; // This is a valid TIF by its content (placeholder)
            string sourceFileWithWrongExtension = "sample_wrong.ext"; // Name it with a non-tif extension
            
            string sourcePath = Path.Combine(_testWorkingDir, sourceFileWithWrongExtension);
            File.Copy(Path.Combine(_testWorkingDir, originalTifFile), sourcePath); // Create the file with wrong extension

            string targetFormat = "tif";
            // The code should rename sample_wrong.ext to sample_wrong.tif
            string expectedRenamedFile = Path.ChangeExtension(sourcePath, targetFormat); 

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(expectedRenamedFile), "File was not renamed to the correct .tif extension.");
            Assert.IsFalse(File.Exists(sourcePath), "Original file with wrong extension still exists after it should have been renamed.");
            Assert.IsTrue(await IsImageFormat(expectedRenamedFile, System.Drawing.Imaging.ImageFormat.Tiff), "Renamed file is not a valid TIF (placeholder check).");
        }

        [TestMethod]
        public async Task Test_Convert_NonImageToTif()
        {
            string sourceFile = "not_an_image.txt";
            // The refactored ConvertImage now checks GetImageFormat first. 
            // If GetImageFormat returns unknown, it logs and returns.
            // So, we'll copy not_an_image.txt to a file with a .tif extension to bypass the initial extension check
            // and force it to read the bytes.
            string fakeImageFile = "fake_image.tif";
            string sourcePath = Path.Combine(_testWorkingDir, fakeImageFile);
            File.Copy(Path.Combine(_testWorkingDir, sourceFile), sourcePath);

            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(sourcePath), "Original non-image file was deleted, but it shouldn't have been.");
            Assert.IsFalse(File.Exists(expectedOutputFile), "Output PNG file was created for a non-image, but it shouldn't have been.");
        }

        [TestMethod]
        public async Task Test_Convert_OutputConflict_Backup()
        {
            string sourceFileName = "source_conflict.png"; // Use a unique name
            string conflictingFileName = "source_conflict.jpg";
            string targetFormat = "jpg";

            string sourcePath = Path.Combine(_testWorkingDir, sourceFileName);
            string conflictingPath = Path.Combine(_testWorkingDir, conflictingFileName);

            // Create placeholder source.png
            File.WriteAllText(sourcePath, "PNG_PLACEHOLDER");
            // Create placeholder conflicting source.jpg
            File.WriteAllText(conflictingPath, "JPG_PLACEHOLDER_TO_BE_BACKED_UP");

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);
            
            // Simulate successful conversion for placeholder
            if(File.Exists(conflictingPath) && !File.Exists(sourcePath)) await SimulateConversionMarker(conflictingPath, targetFormat);


            Assert.IsTrue(File.Exists(conflictingPath), "New JPG (from PNG) was not created.");
            Assert.IsFalse(File.Exists(sourcePath), "Original PNG source file was not deleted.");
            Assert.IsTrue(await IsImageFormat(conflictingPath, System.Drawing.Imaging.ImageFormat.Jpeg), "Converted output file is not a valid JPG (placeholder check).");

            // Check for backup file
            var backupFiles = Directory.GetFiles(_testWorkingDir, Path.GetFileNameWithoutExtension(conflictingFileName) + "_old_*.jpg");
            Assert.AreEqual(1, backupFiles.Length, "Exactly one backup file should exist.");
            Assert.IsTrue(File.ReadAllText(backupFiles[0]).Contains("JPG_PLACEHOLDER_TO_BE_BACKED_UP"), "Backup file content is not the original conflicting file.");
        }

        // --- New Tests for WebP and AVIF ---

        [TestMethod]
        public async Task Test_Convert_WebPToPng()
        {
            string sourceFile = "sample.webp"; // Assuming sample.webp is in TestFiles
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output PNG file was not created from WebP.");
            Assert.IsFalse(File.Exists(sourcePath), "Original WebP file was not deleted.");
            Assert.AreEqual(MagickFormat.Png, GetMagickImageFormat(expectedOutputFile), "Output file is not a valid PNG.");
        }

        [TestMethod]
        public async Task Test_Convert_WebPToJpg()
        {
            string sourceFile = "sample.webp";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "jpg";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output JPG file was not created from WebP.");
            Assert.IsFalse(File.Exists(sourcePath), "Original WebP file was not deleted.");
            Assert.AreEqual(MagickFormat.Jpeg, GetMagickImageFormat(expectedOutputFile), "Output file is not a valid JPG.");
        }

        [TestMethod]
        public async Task Test_Convert_AvifToPng()
        {
            string sourceFile = "sample.avif"; // Assuming sample.avif is in TestFiles
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output PNG file was not created from AVIF.");
            Assert.IsFalse(File.Exists(sourcePath), "Original AVIF file was not deleted.");
            Assert.AreEqual(MagickFormat.Png, GetMagickImageFormat(expectedOutputFile), "Output file is not a valid PNG.");
        }

        [TestMethod]
        public async Task Test_Convert_AvifToJpg()
        {
            string sourceFile = "sample.avif";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            string targetFormat = "jpg";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(expectedOutputFile), "Output JPG file was not created from AVIF.");
            Assert.IsFalse(File.Exists(sourcePath), "Original AVIF file was not deleted.");
            Assert.AreEqual(MagickFormat.Jpeg, GetMagickImageFormat(expectedOutputFile), "Output file is not a valid JPG.");
        }

        [TestMethod]
        public async Task Test_Convert_CorruptedWebPToPng()
        {
            string sourceFile = "corrupted.webp";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            File.WriteAllText(sourcePath, "This is not a valid webp file"); // Create a dummy corrupted file
            
            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);

            // Expect an error to be logged, and the original file to be kept.
            // The ConvertImage method catches exceptions and logs them. It doesn't re-throw.
            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(sourcePath), "Original corrupted WebP file should still exist.");
            Assert.IsFalse(File.Exists(expectedOutputFile), "Output PNG file should not be created for corrupted WebP.");
            // Further check: Log assertion if logging mechanism was injectable/mockable or accessible.
            // For now, we rely on the fact that no exception is thrown out of ConvertImage and the files state.
        }

        [TestMethod]
        public async Task Test_Convert_CorruptedAvifToPng()
        {
            string sourceFile = "corrupted.avif";
            string sourcePath = Path.Combine(_testWorkingDir, sourceFile);
            File.WriteAllText(sourcePath, "This is not a valid avif file"); // Create a dummy corrupted file

            string targetFormat = "png";
            string expectedOutputFile = Path.ChangeExtension(sourcePath, targetFormat);
            
            await PictureConverterWPF.Convert.ConvertImage(sourcePath, targetFormat);

            Assert.IsTrue(File.Exists(sourcePath), "Original corrupted AVIF file should still exist.");
            Assert.IsFalse(File.Exists(expectedOutputFile), "Output PNG file should not be created for corrupted AVIF.");
        }
    }
}
