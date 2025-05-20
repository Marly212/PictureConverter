# PictureConverter
Simple Picture Converter in C#

## Features
*   Convert images from popular formats like JPG, PNG, GIF, WEBP, and AVIF.
*   Output images in JPG or PNG formats.
*   Select individual image files or entire folders for batch conversion.
*   Optionally include subfolders when converting entire directories.
*   User-friendly graphical interface.

## How to use
Download the latest release from the [Releases page](https://github.com/Marly212/PictureConverter/releases) and extract the archive to a folder on your computer.

Navigate into the extracted folder. To start the application, double-click `PictureConverterWPF.exe`.

The interface is designed to be intuitive:
1.  Use the **Browse File** button to select one or more image files, or the **Browse Folder** button to select a directory of images.
2.  If converting a folder, you can check the **Include Subfolders** option to process images in subdirectories as well.
3.  Select your desired output format (PNG or JPG) from the dropdown menu.
4.  Click the **Start** button to begin the conversion.

A progress bar will show the status of the conversion. A pop-up message will notify you when the process is complete.

## Currently supported image formats
### Convert from
| Format        | Supported     |
| :------------- | :----------: |
|  JPG | :heavy_check_mark:   | 
| PNG   | :heavy_check_mark: |
| WEBP   | :heavy_check_mark: |
| AVIF   | :heavy_check_mark: |
| GIF   | :heavy_check_mark: |


### Convert to
| Format        | Supported     |
| :------------- | :----------: |
|  JPG | :heavy_check_mark:   | 
| PNG   | :heavy_check_mark: |
| WEBP   | Not Supported |

## Dependencies
This application relies on the following bundled command-line utilities for handling specific image formats:
*   `dwebp.exe`: For decoding WEBP images.
*   `avifdec.exe`: For decoding AVIF images.
These tools are included with the application, and no separate installation is required.

## Building from Source
This project is a .NET 6 WPF application. To build it from source:
1.  Ensure you have the .NET 6 SDK installed.
2.  You will also need Visual Studio 2022 (Community Edition is sufficient) with the ".NET desktop development" workload installed.
3.  Clone this repository.
4.  Open the `PictureConverterGUI.sln` solution file in Visual Studio.
5.  Dependencies, such as `Ookii.Dialogs.Wpf`, are managed via NuGet and should be restored automatically upon build.

## License

[GPL3.0](LICENSE)

