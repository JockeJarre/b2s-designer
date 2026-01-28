# B2S File Format Library and Converter

This folder contains the B2S file format library and command-line converter tool.

## Projects

### B2SFileFormat.Library

A reusable VB.NET class library for reading and writing B2S backglass files in both directb2s (XML) and B2Sz (ZIP) formats.

**Targets:**
- .NET Framework 4.8 (for compatibility with B2S Designer)
- .NET 8.0 (for modern cross-platform use)

**Dependencies:**
- System.IO.Compression (for ZIP file handling)

**Usage:**

```vb.net
' Load a file (auto-detects format)
Dim b2sFile As B2SFile = B2SFile.Load("mybackglass.directb2s")

' Convert to B2Sz
b2sFile.SaveB2Sz("mybackglass.B2Sz")

' Convert back to directb2s
Dim b2sFile2 As B2SFile = B2SFile.Load("mybackglass.B2Sz")
b2sFile2.SaveDirectB2S("restored.directb2s")

' Access the XML document
Dim xmlDoc As XmlDocument = b2sFile.XmlDocument

' Access extracted images
For Each kvp In b2sFile.Images
    Console.WriteLine($"Image: {kvp.Key}, Size: {kvp.Value.Length} bytes")
Next
```

### B2SConverter

A command-line tool for converting between directb2s and B2Sz formats.

**Usage:**

```bash
# Convert from directb2s to B2Sz
dotnet B2SConverter.dll input.directb2s output.B2Sz

# Convert from B2Sz to directb2s
dotnet B2SConverter.dll input.B2Sz output.directb2s

# On Windows with .NET Framework
B2SConverter.exe input.directb2s output.B2Sz
```

**Output Example:**

```
B2S File Format Converter v1.1
Converts between directb2s and B2Sz formats

Input:  test.directb2s
Output: test.B2Sz

Loading input file...
Loaded successfully. Found 1 images.
Converting...
Saving as B2Sz (ZIP with separate image files)...

Conversion completed successfully!
Input size:  1.77 KB
Output size: 953.00 B

Note: The B2Sz format stores images as separate files in the ZIP archive,
which typically results in smaller file sizes due to PNG compression.
```

## Building

### Build Both Projects

```bash
cd B2SFileFormat
dotnet build -c Release
```

### Build Individual Projects

```bash
# Library only
cd B2SFileFormat.Library
dotnet build -c Release

# Converter only
cd B2SConverter
dotnet build -c Release
```

## Binary Locations

After building:

**Library:**
- .NET Framework 4.8: `B2SFileFormat.Library/bin/Release/net48/B2SFileFormat.Library.dll`
- .NET 8.0: `B2SFileFormat.Library/bin/Release/net8.0/B2SFileFormat.Library.dll`

**Converter:**
- .NET Framework 4.8: `B2SConverter/bin/Release/net48/B2SConverter.exe`
- .NET 8.0: `B2SConverter/bin/Release/net8.0/B2SConverter.dll`

## File Format Details

### directb2s Format

- Traditional B2S backglass format
- XML file with base64-encoded images embedded in `Image` attributes
- Single file, easy to distribute
- Larger file size due to base64 encoding (~33% size increase)

### B2Sz Format

- New ZIP-based format
- Contains:
  - XML file with structure (Image attributes are empty, filenames in FileName attributes)
  - Individual image files in their native format
- Smaller file size (typically 25-50% smaller than directb2s)
- Easier to extract and modify images
- Better for version control systems

## Integration with Other Projects

To use the library in your own project:

```xml
<!-- For .NET Framework 4.8 projects -->
<ItemGroup>
  <Reference Include="B2SFileFormat.Library">
    <HintPath>..\path\to\B2SFileFormat.Library.dll</HintPath>
  </Reference>
</ItemGroup>

<!-- For .NET 8.0+ projects -->
<ItemGroup>
  <Reference Include="B2SFileFormat.Library" />
</ItemGroup>
```

Or add as a NuGet package reference once published.

## Testing

The converter has been tested with:
- Simple files (1 image): ~47% file size reduction
- Complex files (6+ images): ~27% file size reduction
- Round-trip conversion preserves all data and XML structure
- All image types (PNG, JPEG, GIF, BMP) supported

## License

This library follows the same license as the main B2S Designer project.
