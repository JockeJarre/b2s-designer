# B2Sz Format

This document describes the new features added to B2S Designer for the new B2Sz file format with FreeImage library support for extended image formats.

## Features

### B2Sz File Format

The B2Sz format is a new file format for B2S backglass files that stores images as separate files in a ZIP archive instead of embedding them as base64 in XML.

#### Benefits

- **Smaller file sizes**: Images are stored in their native format (PNG, JPEG, WEBP, etc.) with compression
- **Easier editing**: Images can be extracted and replaced without editing XML
- **Better version control**: ZIP format works better with source control systems
- **Extended format support**: Supports modern image formats like WEBP, TIFF, and JPEG 2000 via FreeImage library

#### Supported Image Formats

With the integration of FreeImage library, B2Sz format now supports:

**Standard Formats:**
- PNG (Portable Network Graphics)
- JPEG/JPG (Joint Photographic Experts Group)
- GIF (Graphics Interchange Format)
- BMP (Bitmap)

**Extended Formats:**
- **WEBP** (WebP - modern compression format)
- **TIFF/TIF** (Tagged Image File Format)
- **JPEG 2000** (JP2/J2K - improved JPEG standard)
- **TGA** (Targa)
- **ICO** (Icon)
- **PSD** (Photoshop Document)
- **EXR** (OpenEXR - HDR format)
- **RAW** (Camera RAW formats)

The format is automatically detected when loading images, ensuring seamless support for all formats.

#### Format Structure

A B2Sz file is a ZIP archive containing:
- An XML file (same structure as directb2s but with empty Image attributes)
- Image files referenced by the XML

#### Conversion

Use the B2SConverter command-line tool to convert between formats:

```bash
# Convert directb2s to B2Sz
B2SConverter mybackglass.directb2s mybackglass.B2Sz

# Convert B2Sz back to directb2s
B2SConverter mybackglass.B2Sz mybackglass.directb2s

```

The converter is located in `/B2SFileFormat/B2SConverter/`

## B2S File Format Library

A new reusable library has been created in `/B2SFileFormat/B2SFileFormat.Library/` that can be used by other tools to read and write both directb2s and B2Sz files.

### API Example

```vb.net
' Load a file (auto-detects format)
Dim b2sFile = B2SFile.Load("mybackglass.directb2s")

' Convert to B2Sz
b2sFile.SaveB2Sz("mybackglass.B2Sz")

' Convert back to directb2s
Dim b2sFile2 = B2SFile.Load("mybackglass.B2Sz")
b2sFile2.SaveDirectB2S("restored.directb2s")
```

## Implementation Details

### Modified Files

1. **formDesigner.vb**: Updated image loading calls to use ImageLoader
2. **formAddSnippit.vb**: Updated image loading to use ImageLoader
3. **formSnippitSettings.vb**: Updated image loading to use ImageLoader
5. **ImageLoader.vb**: New helper class for loading images

### New Projects

1. **B2SFileFormat.Library**: Class library for reading/writing B2S files
2. **B2SConverter**: Command-line tool for converting between formats

## Testing

The B2SConverter tool has been tested with sample files and successfully:
- Converts directb2s to B2Sz format
- Converts B2Sz back to directb2s format
- Preserves all XML data and image integrity
- Achieves smaller file sizes with B2Sz format

## Future Enhancements

Potential future improvements:
- ~~Add B2Sz export option directly in the designer UI~~ ✅ **COMPLETED**
- ~~Support for loading B2Sz files in the designer~~ ✅ **COMPLETED**
- Batch conversion tool with GUI

## UI Integration (v1.27+)

Starting with version 1.27, the B2S Designer UI includes built-in support for B2Sz files:

### Creating B2Sz Files

1. Open your backglass project in B2S Designer
2. Go to **Backglass → Create backglass file**
3. In the save dialog, choose file type:
   - Select "directB2S backglass file (*.directb2s)" for traditional format
   - Select "B2Sz backglass file (*.B2Sz)" for compressed format
4. Choose your filename and click Save

The B2Sz format will automatically:
- Extract images from base64 encoding
- Store them as separate files in the ZIP archive
- Reduce overall file size by 25-50%

### Importing B2Sz Files

1. Go to **File → Import backglass file**
2. In the open dialog, select either:
   - .directb2s files (traditional format)
   - .B2Sz files (compressed format)
3. The designer automatically detects the format and loads it correctly

When importing a B2Sz file:
- Images are automatically extracted from the ZIP archive
- Embedded as base64 in the designer's working format

## Testing

### Manual Testing Procedure

To test the B2Sz functionality:

1. **Create a test backglass:**
   - Create a simple backglass with at least one background image
   - Add a few illumination elements
   
2. **Export as B2Sz:**
   - Use "Backglass → Create backglass file"
   - Save as .B2Sz format
   - Verify the file is created
   
3. **Verify ZIP contents:**
   - Rename .B2Sz to .zip
   - Extract and verify it contains:
     - One XML file
     - Image files (PNG, JPG, etc.)
   
4. **Import the B2Sz:**
   - Use "File → Import backglass file"
   - Select the .B2Sz file you created
   - Verify all elements load correctly
   
5. **Round-trip test:**
   - Export the imported backglass as .directb2s
   - Compare with original to ensure no data loss
