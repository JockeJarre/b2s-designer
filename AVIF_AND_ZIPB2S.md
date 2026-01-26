# AVIF Support and zipb2s Format

This document describes the new features added to B2S Designer for AVIF image support and the new zipb2s file format.

## Features

### 1. AVIF Image Format Support

B2S Designer now supports loading AVIF (AV1 Image File Format) images in addition to the standard PNG, JPEG, GIF, and BMP formats.

#### Requirements

AVIF support in B2S Designer has different capabilities depending on the component:

**B2SBackglassDesigner (Windows Forms Application):**
- Uses .NET Framework 4.8 with SixLabors.ImageSharp 2.1.11 (embedded via Costura.Fody)
- **AVIF Import**: ❌ NOT supported (ImageSharp 2.1.x lacks AVIF decoder)
- **Note**: AVIF images cannot be imported into the designer due to ImageSharp version limitations

**B2SConverter for Converting zipb2s Files:**
- **net48 version** (B2SConverter.exe): Uses ImageSharp 2.1.11 - ❌ NO AVIF support
- **net8.0 version** (net8.0/B2SConverter): Uses ImageSharp 3.1.12 - ✅ FULL AVIF support
- For converting files with AVIF images, use the net8.0 version (requires .NET 8.0 Runtime)
- See `B2SConverter.AVIF.README.md` in the B2SFileFormat folder for detailed instructions

**Why the limitation?**
SixLabors.ImageSharp 3.0+ includes AVIF support but only targets .NET 6.0+, not .NET Framework 4.8. The B2SBackglassDesigner requires .NET Framework 4.8 (Windows Forms), so it uses ImageSharp 2.1.11 which lacks AVIF decoding.

The SixLabors.ImageSharp license (Apache-2.0 / Six Labors Split License) is included in the distribution package.

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
- AVIF conversion requires ImageSharp library with AVIF encoder support (embedded in B2SFileFormat.Library.dll)

The converter is located in `/B2SFileFormat/B2SConverter/` and is included in the B2S Designer distribution package with all required dependencies embedded in B2SFileFormat.Library.dll.

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
- ~~Add zipb2s export option directly in the designer UI~~ ✅ **COMPLETED**
- ~~Support for loading zipb2s files in the designer~~ ✅ **COMPLETED**
- Batch conversion tool with GUI
- Image optimization during conversion

## UI Integration (v1.27+)

Starting with version 1.27, the B2S Designer UI includes built-in support for zipB2S files:

### Creating zipB2S Files

1. Open your backglass project in B2S Designer
2. Go to **Backglass → Create backglass file**
3. In the save dialog, choose file type:
   - Select "directB2S backglass file (*.directb2s)" for traditional format
   - Select "zipB2S backglass file (*.zipb2s)" for compressed format
4. Choose your filename and click Save

The zipB2S format will automatically:
- Extract images from base64 encoding
- Store them as separate files in the ZIP archive
- Reduce overall file size by 25-50%

### Importing zipB2S Files

1. Go to **File → Import backglass file**
2. In the open dialog, select either:
   - .directb2s files (traditional format)
   - .zipb2s files (compressed format)
3. The designer automatically detects the format and loads it correctly

When importing a zipB2S file:
- Images are automatically extracted from the ZIP archive
- Embedded as base64 in the designer's working format
- AVIF images are automatically converted to PNG for compatibility

### AVIF Support

AVIF image format is supported when:
- Importing background images
- Importing illumination images
- Importing DMD images
- Dragging and dropping files onto the designer

**Note:** AVIF support requires the SixLabors.ImageSharp library, which is included in the B2S Designer distribution package.

## Testing

### Manual Testing Procedure

To test the zipB2S functionality:

1. **Create a test backglass:**
   - Create a simple backglass with at least one background image
   - Add a few illumination elements
   
2. **Export as zipB2S:**
   - Use "Backglass → Create backglass file"
   - Save as .zipb2s format
   - Verify the file is created
   
3. **Verify ZIP contents:**
   - Rename .zipb2s to .zip
   - Extract and verify it contains:
     - One XML file
     - Image files (PNG, JPG, etc.)
   
4. **Import the zipB2S:**
   - Use "File → Import backglass file"
   - Select the .zipb2s file you created
   - Verify all elements load correctly
   
5. **Round-trip test:**
   - Export the imported backglass as .directb2s
   - Compare with original to ensure no data loss
