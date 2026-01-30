# FreeImage Library Integration

This document describes the integration of FreeImage library into B2S Designer for extended image format support.

## Overview

B2S Designer now uses the **FreeImage library** (via FreeImage.Standard NuGet package v4.3.8) to provide support for modern and extended image formats including **WEBP**, TIFF, JPEG 2000, and many others.

## Library Selection

**Selected: FreeImage.Standard v4.3.8**

### Why FreeImage.Standard?

1. **Comprehensive Format Support**: FreeImage supports over 30 image formats including modern standards like **WEBP**
2. **Native Performance**: Uses native FreeImage.dll for optimal performance
3. **Industry Standard**: Widely used in professional applications
4. **Open Source**: Free to use with proper licensing
5. **Active Development**: Well-maintained .NET wrapper
6. **No Security Vulnerabilities**: Clean security scan compared to alternatives

### Alternatives Considered

- **ImageSharp**: Removed per requirements
- **Magick.NET**: Many known security vulnerabilities  
- **FreeImage-dotnet-core**: Older version without WEBP support
- **System.Drawing**: Limited format support (built-in fallback)

## Integration Details

### Components

1. **B2SFileFormat.Library** (.NET Standard multi-target):
   - FreeImage.Standard package reference
   - Enhanced format detection in `DetectImageFormat()`
   - Automatic format identification for B2Sz conversion

2. **B2SBackglassDesigner** (.NET Framework 4.8):
   - FreeImage.Standard package reference
   - Enhanced `ImageLoader.vb` with FreeImage support
   - Seamless loading of extended formats

### Supported Formats

| Format | Extension | Description | Support Level |
|--------|-----------|-------------|---------------|
| PNG | .png | Portable Network Graphics | Full |
| JPEG | .jpg, .jpeg | Joint Photographic Experts Group | Full |
| GIF | .gif | Graphics Interchange Format | Full |
| BMP | .bmp | Windows Bitmap | Full |
| **WEBP** | **.webp** | **Google WebP (modern compression)** | **Full** ✅ |
| **TIFF** | **.tiff, .tif** | **Tagged Image File Format** | **Full** |
| **JPEG 2000** | **.jp2, .j2k** | **Improved JPEG standard** | **Full** |
| **JXR** | **.jxr** | **JPEG XR (HD Photo)** | **Full** |
| TGA | .tga | Truevision Targa | Full |
| ICO | .ico | Windows Icon | Full |
| PSD | .psd | Adobe Photoshop Document | Read-only |
| EXR | .exr | OpenEXR (HDR) | Full |

**Bold** formats indicate new capabilities added with FreeImage integration.

## WEBP Support ✅

**WEBP is fully supported** through FreeImage.Standard! This modern image format offers:
- Superior compression (25-35% smaller than JPEG/PNG)
- Both lossy and lossless compression
- Transparency support (like PNG)
- Animation support (like GIF)

Perfect for reducing B2Sz file sizes while maintaining quality.

## Usage

### For End Users

1. **Loading Images**: 
   - Simply use File → Open or drag and drop
   - All supported formats work automatically
   - No manual conversion needed

2. **Saving Projects**:
   - Use B2Sz format for maximum compatibility
   - Images are stored in their original format
   - Automatic format detection and optimization

3. **Converting Formats**:
   - Import any supported format
   - Export to B2Sz (preserves format)
   - Export to directb2s (converts to base64)

### For Developers

```vb.net
' Load any supported image format
Dim image As Image = ImageLoader.LoadImage("myimage.webp")

' Check if format is supported
If ImageLoader.IsSupportedFormat("myimage.webp") Then
    ' Process image
End If

' Get format information
Dim formatName As String = ImageLoader.GetImageFormatName("myimage.webp")
```

### B2Sz Format Integration

When saving to B2Sz format, images are:
1. Automatically detected for format
2. Stored in their native format (WEBP stays WEBP, TIFF stays TIFF, etc.)
3. Compressed within ZIP archive
4. Referenced by filename in XML

## Technical Implementation

### Format Detection Flow

```
1. FreeImage.GetFileTypeFromStream() - Primary detection
2. Manual signature detection - Fallback for WEBP and other formats
3. Default to PNG - Safe fallback
```

### Image Loading Flow

```
1. FreeImage.LoadEx() - Load image using FreeImage
2. FreeImage.GetBitmap() - Convert to System.Drawing.Bitmap
3. Bitmap copy - Create independent instance
4. FreeImage.UnloadEx() - Clean up FreeImage resources
5. Fallback to Bitmap.FromFile() if FreeImage fails
```

### Memory Management

- FreeImage bitmaps are properly disposed after conversion
- Copies are created to prevent locking
- Resources are released promptly

## Native Dependencies

### FreeImage.dll Distribution

The FreeImage-dotnet-core package includes native FreeImage.dll for:
- **Windows**: FreeImage.dll (x86 and x64)
- **Linux**: libfreeimage.so
- **macOS**: libfreeimage.dylib

These are automatically copied to the output directory during build.

### Deployment Checklist

When distributing B2S Designer:
- ✅ Include FreeImage.dll (from packages/FreeImage-dotnet-core.4.3.6/runtimes/)
- ✅ Include FreeImageNET.dll
- ✅ Ensure both are in the same directory as B2SBackglassDesigner.exe

## Performance Considerations

1. **Initial Load**: FreeImage.dll is loaded on first use
2. **Format Detection**: Very fast (reads file headers only)
3. **Image Loading**: Comparable to System.Drawing for standard formats
4. **Extended Formats**: WEBP and JPEG 2000 may be slower than PNG/JPEG

## Backwards Compatibility

- **Full backwards compatibility** with existing directb2s and B2Sz files
- Standard formats (PNG, JPEG, GIF, BMP) work identically
- Extended formats are optional - users can continue using standard formats
- FreeImage fallback to System.Drawing ensures reliability

## Future Enhancements

Potential improvements:
- AVIF format support (when FreeImage adds it)
- HEIF/HEIC format support
- Batch conversion tool for image format optimization
- Format-specific save options (compression levels, etc.)

## Licensing

**FreeImage Library**: 
- Licensed under GPLv3 or FreeImage Public License (FIPL)
- Free for open-source projects
- Commercial license may be required for proprietary use

**FreeImage-dotnet-core**:
- MIT License wrapper
- Open source and free to use

Ensure compliance with FreeImage licensing when distributing B2S Designer.

## Troubleshooting

### "FreeImage.dll not found"
- Ensure FreeImage.dll is in the application directory
- Check architecture matches (x86 vs x64)
- Reinstall NuGet packages

### "Format not supported"
- Check format is in supported list
- Update FreeImage-dotnet-core to latest version
- Use fallback formats (PNG, JPEG)

### "Out of memory" errors
- Large images may require more memory
- Consider downscaling before import
- Use more efficient formats (WEBP instead of TIFF)

## References

- [FreeImage Project](https://freeimage.sourceforge.io/)
- [FreeImage-dotnet-core on NuGet](https://www.nuget.org/packages/FreeImage-dotnet-core/)
- [B2Sz File Format Documentation](B2Sz_file_format.md)
