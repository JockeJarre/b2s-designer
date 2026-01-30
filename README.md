# B2S (Backglass 2nd Screen) Designer

It allows you to edit and create directB2S and B2Sz backglasses for the [B2S.Server](https://github.com/vpinball/b2s-backglass) using a "WYSIWYG" editor.

## Features

- **Visual Editor**: WYSIWYG interface for designing backglasses
- **Multiple Formats**: Support for directB2S and B2Sz file formats
  - **directB2S**: Traditional XML format with embedded base64 images
  - **B2Sz**: New ZIP-based format with 25-50% smaller file sizes
- **Extended Image Format Support**: 
  - **WEBP** ✅ - Modern compression format (25-35% smaller files)
  - **TIFF** - High-quality image format
  - **JPEG 2000** - Advanced JPEG standard
  - **Standard formats**: PNG, JPEG, GIF, BMP
  - Powered by **FreeImage library** for comprehensive format support
- **Animation Support**: Create animations for bulbs and other elements
- **VPinMAME Integration**: Direct ROM communication for authentic pinball behavior

See [B2Sz_file_format.md](B2Sz_file_format.md) for detailed information about the B2Sz format.
See [FREEIMAGE_INTEGRATION.md](FREEIMAGE_INTEGRATION.md) for details about FreeImage library integration and supported formats.

Documentation is available in the package as html-help but can also be read [online here](https://htmlpreview.github.io/?https://raw.githubusercontent.com/vpinball/b2s-designer/master/b2sbackglassdesigner/b2sbackglassdesigner/htmlhelp/Introduction.htm).

**NOTE** If you are having problem opening the html-help B2SBackglassDesigner.chm file, try right click and select Unblock. *This is a security feature of Windows*

## Developers

To build either B2S.Server or B2S.Designer, open the *.sln files in Visual Studio (any version from 2019 should work) and select **build**->**Build Solution** from the menu.
Microsoft has prepared VM's that include Windows 11 and Visual Studio in one bundle. The licence for these is usually 3 months. This is an easy way to test if it works without having to install lots of different components.

## Linux users

You can run this application on linux with wine:

```shell-session
export WINEPREFIX=./wine
export WINEARCH=win32
winetricks dotnet40
winetricks gdiplus
# I could not make it run with wine-mono but it was fine with dotnet
winetricks remove_mono
winetricks dotnet48

# The application can now be started with
wine B2SBackglassDesigner.exe
```
