Imports System
Imports System.IO
Imports B2SFileFormat.Library

Module Program
    Sub Main(args As String())
        Console.WriteLine("B2S File Format Converter v1.1")
        Console.WriteLine("Converts between directb2s and zipb2s formats")
        Console.WriteLine()

        If args.Length < 2 Then
            ShowUsage()
            Environment.Exit(1)
        End If

        ' Parse arguments
        Dim convertToAvif As Boolean = False
        Dim inputFile As String = Nothing
        Dim outputFile As String = Nothing
        
        Dim argIndex = 0
        While argIndex < args.Length
            If args(argIndex) = "--convert-to-avif" OrElse args(argIndex) = "-a" Then
                convertToAvif = True
            ElseIf inputFile Is Nothing Then
                inputFile = args(argIndex)
            ElseIf outputFile Is Nothing Then
                outputFile = args(argIndex)
            End If
            argIndex += 1
        End While

        If inputFile Is Nothing OrElse outputFile Is Nothing Then
            ShowUsage()
            Environment.Exit(1)
        End If

        Try
            ' Validate input file exists
            If Not File.Exists(inputFile) Then
                Console.WriteLine($"Error: Input file not found: {inputFile}")
                Environment.Exit(1)
            End If

            ' Determine conversion direction based on file extensions
            Dim inputExt = Path.GetExtension(inputFile).ToLower()
            Dim outputExt = Path.GetExtension(outputFile).ToLower()

            Console.WriteLine($"Input:  {inputFile}")
            Console.WriteLine($"Output: {outputFile}")
            If convertToAvif Then
                Console.WriteLine("Option: Convert PNG images to AVIF format")
            End If
            Console.WriteLine()

            ' Load the input file
            Console.WriteLine("Loading input file...")
            Dim b2sFile As B2SFile = B2SFile.Load(inputFile)
            Console.WriteLine($"Loaded successfully. Found {b2sFile.Images.Count} images.")

            ' Convert PNG to AVIF if requested
            If convertToAvif AndAlso outputExt = ".zipb2s" Then
                Console.WriteLine("Converting PNG images to AVIF...")
                ConvertPngImagesToAvif(b2sFile)
            End If

            ' Save in the target format
            Console.WriteLine("Converting...")
            If outputExt = ".directb2s" Then
                Console.WriteLine("Saving as directb2s (XML with embedded base64 images)...")
                If convertToAvif Then
                    Console.WriteLine("Note: AVIF images will be converted to PNG for directb2s compatibility.")
                End If
                b2sFile.SaveDirectB2S(outputFile)
            ElseIf outputExt = ".zipb2s" Then
                Console.WriteLine("Saving as zipb2s (ZIP with separate image files)...")
                b2sFile.SaveZipB2S(outputFile)
            Else
                Console.WriteLine($"Error: Unsupported output format: {outputExt}")
                Console.WriteLine("Supported formats: .directb2s, .zipb2s")
                Environment.Exit(1)
            End If

            Console.WriteLine()
            Console.WriteLine("Conversion completed successfully!")
            
            ' Show file sizes
            Dim inputSize = New FileInfo(inputFile).Length
            Dim outputSize = New FileInfo(outputFile).Length
            Console.WriteLine($"Input size:  {FormatBytes(inputSize)}")
            Console.WriteLine($"Output size: {FormatBytes(outputSize)}")

            If outputExt = ".zipb2s" Then
                Console.WriteLine()
                Console.WriteLine("Note: The zipb2s format stores images as separate files in the ZIP archive.")
                If convertToAvif Then
                    Console.WriteLine("AVIF format provides better compression than PNG for most images.")
                End If
            End If

        Catch ex As Exception
            Console.WriteLine()
            Console.WriteLine($"Error: {ex.Message}")
            Console.WriteLine()
            Console.WriteLine("Stack trace:")
            Console.WriteLine(ex.StackTrace)
            Environment.Exit(1)
        End Try
    End Sub

    Sub ConvertPngImagesToAvif(b2sFile As B2SFile)
        Dim converted = 0
        Dim keys = b2sFile.Images.Keys.ToList()
        
        For Each key In keys
            If Path.GetExtension(key).ToLower() = ".png" Then
                Try
                    Dim pngBytes = b2sFile.Images(key)
                    Dim avifBytes = B2SFile.ConvertPngToAvif(pngBytes)
                    
                    ' Replace the PNG with AVIF
                    b2sFile.Images.Remove(key)
                    Dim newKey = Path.ChangeExtension(key, ".avif")
                    b2sFile.Images(newKey) = avifBytes
                    converted += 1
                    Console.WriteLine($"  Converted: {key} -> {newKey}")
                Catch ex As Exception
                    Console.WriteLine($"  Warning: Failed to convert {key}: {ex.Message}")
                End Try
            End If
        Next
        
        Console.WriteLine($"Converted {converted} PNG image(s) to AVIF format.")
    End Sub

    Sub ShowUsage()
        Console.WriteLine("Usage: B2SConverter [options] <input-file> <output-file>")
        Console.WriteLine()
        Console.WriteLine("Options:")
        Console.WriteLine("  --convert-to-avif, -a   Convert PNG images to AVIF format (zipb2s output only)")
        Console.WriteLine()
        Console.WriteLine("Supported formats:")
        Console.WriteLine("  .directb2s  - XML format with base64 embedded images")
        Console.WriteLine("  .zipb2s     - ZIP format with XML and separate image files")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  B2SConverter mybackglass.directb2s mybackglass.zipb2s")
        Console.WriteLine("  B2SConverter mybackglass.zipb2s mybackglass.directb2s")
        Console.WriteLine("  B2SConverter --convert-to-avif mybackglass.directb2s mybackglass.zipb2s")
        Console.WriteLine()
        Console.WriteLine("Notes:")
        Console.WriteLine("  - AVIF images in zipb2s are preserved in their original format")
        Console.WriteLine("  - When converting zipb2s to directb2s, AVIF images are converted to PNG")
        Console.WriteLine("  - AVIF conversion requires ImageSharp library with AVIF encoder support")
    End Sub

    Function FormatBytes(bytes As Long) As String
        Dim suffixes() As String = {"B", "KB", "MB", "GB"}
        Dim counter As Integer = 0
        Dim number As Decimal = bytes
        While number >= 1024 AndAlso counter < suffixes.Length - 1
            number = number / 1024
            counter += 1
        End While
        Return $"{number:N2} {suffixes(counter)}"
    End Function
End Module

