# B2SConverter - AVIF Support

The B2S Designer distribution includes two versions of B2SConverter:

## B2SConverter.exe (Root Directory)
- **Platform**: .NET Framework 4.8
- **ImageSharp Version**: 2.1.11
- **AVIF Support**: ❌ NO (ImageSharp 2.1.x does not include AVIF decoder)
- **Use for**: General conversions between directb2s and zipb2s formats without AVIF images

## net8.0/B2SConverter (Subdirectory)
- **Platform**: .NET 8.0
- **ImageSharp Version**: 3.1.12
- **AVIF Support**: ✅ YES (Full AVIF encoding and decoding support)
- **Use for**: Converting files with AVIF images

### Running the .NET 8.0 Version

**Windows:**
```cmd
net8.0\B2SConverter.exe input.zipb2s output.directb2s
```

**Cross-platform (with .NET 8.0 Runtime installed):**
```bash
dotnet net8.0/B2SConverter.dll input.zipb2s output.directb2s
```

### Why Two Versions?

- **Compatibility**: The .NET Framework 4.8 version runs on any Windows system without additional runtime installation
- **Features**: The .NET 8.0 version includes AVIF support, but requires .NET 8.0 runtime to be installed

SixLabors.ImageSharp added AVIF support in version 3.0, but version 3.x only targets .NET 6.0+. Version 2.1.11 (used in the .NET Framework 4.8 build) does not include AVIF codec.

### Installation of .NET 8.0 Runtime

If you want to use AVIF support:
1. Download and install the .NET 8.0 Runtime from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Use the net8.0 version of B2SConverter for files containing AVIF images
