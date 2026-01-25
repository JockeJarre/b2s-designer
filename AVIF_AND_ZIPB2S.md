# AVIF Support and zipb2s Format

This document describes the new features added to B2S Designer for AVIF image support and the new zipb2s file format.

## Features

### 1. AVIF Image Format Support

B2S Designer now supports loading AVIF (AV1 Image File Format) images in addition to the standard PNG, JPEG, GIF, and BMP formats.

#### Requirements

AVIF support requires the SixLabors.ImageSharp library. To enable AVIF support:

1. Add the SixLabors.ImageSharp NuGet package to the B2SBackglassDesigner project:
   - In Visual Studio: Right-click on the project → Manage NuGet Packages
   - Search for "SixLabors.ImageSharp" and install it
   - Or use Package Manager Console: `Install-Package SixLabors.ImageSharp`

2. The ImageLoader class will automatically detect if ImageSharp is available and use it for loading AVIF files

#### Usage

Once ImageSharp is installed:
- Use File → Import Background Image and select an AVIF file
- Use File → Import Illumination Image and select an AVIF file  
- Use File → Import DMD Image and select an AVIF file
- Drag and drop AVIF files onto the designer

AVIF images are converted to standard Bitmap format internally and embedded in the directb2s file as base64-encoded PNG data.

### 2. zipb2s File Format

The zipb2s format is a new file format for B2S backglass files that stores images as separate files in a ZIP archive instead of embedding them as base64 in XML.

#### Benefits

- **Smaller file sizes**: Images are stored in their native format (PNG, JPEG, etc.) with compression
- **Easier editing**: Images can be extracted and replaced without editing XML
- **Better version control**: ZIP format works better with source control systems

#### Format Structure

A zipb2s file is a ZIP archive containing:
- An XML file (same structure as directb2s but with empty Image attributes)
- Image files referenced by the XML

#### Conversion

Use the B2SConverter command-line tool to convert between formats:

```bash
# Convert directb2s to zipb2s
B2SConverter mybackglass.directb2s mybackglass.zipb2s

# Convert zipb2s back to directb2s
B2SConverter mybackglass.zipb2s mybackglass.directb2s

# Convert directb2s to zipb2s with PNG to AVIF conversion
B2SConverter --convert-to-avif mybackglass.directb2s mybackglass.zipb2s
```

**AVIF Image Handling:**
- AVIF images in zipb2s files are preserved in their original format
- When converting zipb2s to directb2s, AVIF images are automatically converted to PNG (since base64 embedding works best with standard formats)
- Use the `--convert-to-avif` flag to convert PNG images to AVIF when creating zipb2s files for better compression
- AVIF conversion requires ImageSharp library with AVIF encoder support

The converter is located in `/B2SFileFormat/B2SConverter/`

## B2S File Format Library

A new reusable library has been created in `/B2SFileFormat/B2SFileFormat.Library/` that can be used by other tools to read and write both directb2s and zipb2s files.

### API Example

```vb.net
' Load a file (auto-detects format)
Dim b2sFile = B2SFile.Load("mybackglass.directb2s")

' Convert to zipb2s
b2sFile.SaveZipB2S("mybackglass.zipb2s")

' Convert back to directb2s
Dim b2sFile2 = B2SFile.Load("mybackglass.zipb2s")
b2sFile2.SaveDirectB2S("restored.directb2s")
```

## Implementation Details

### Modified Files

1. **moduleB2S.vb**: Updated ImageFileExtensionFilter to include AVIF
2. **formDesigner.vb**: Updated image loading calls to use ImageLoader
3. **formAddSnippit.vb**: Updated image loading to use ImageLoader
4. **formSnippitSettings.vb**: Updated image loading to use ImageLoader
5. **ImageLoader.vb**: New helper class for loading images including AVIF

### New Projects

1. **B2SFileFormat.Library**: Class library for reading/writing B2S files
2. **B2SConverter**: Command-line tool for converting between formats

## Testing

The B2SConverter tool has been tested with sample files and successfully:
- Converts directb2s to zipb2s format
- Converts zipb2s back to directb2s format
- Preserves all XML data and image integrity
- Achieves smaller file sizes with zipb2s format

## Future Enhancements

Potential future improvements:
- Add zipb2s export option directly in the designer UI
- Support for loading zipb2s files in the designer
- Batch conversion tool with GUI
- Image optimization during conversion
