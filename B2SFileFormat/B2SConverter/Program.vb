Imports System
Imports System.IO
Imports B2SFileFormat.Library

Module Program
    Sub Main(args As String())
        Console.WriteLine("B2S File Format Converter v1.0")
        Console.WriteLine("Converts between directb2s and zipb2s formats")
        Console.WriteLine()

        If args.Length < 2 Then
            ShowUsage()
            Environment.Exit(1)
        End If

        Dim inputFile = args(0)
        Dim outputFile = args(1)

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
            Console.WriteLine()

            ' Load the input file
            Console.WriteLine("Loading input file...")
            Dim b2sFile As B2SFile = B2SFile.Load(inputFile)
            Console.WriteLine($"Loaded successfully. Found {b2sFile.Images.Count} images.")

            ' Save in the target format
            Console.WriteLine("Converting...")
            If outputExt = ".directb2s" Then
                Console.WriteLine("Saving as directb2s (XML with embedded base64 images)...")
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
                Console.WriteLine("Note: The zipb2s format stores images as separate files in the ZIP archive,")
                Console.WriteLine("which typically results in smaller file sizes due to PNG compression.")
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

    Sub ShowUsage()
        Console.WriteLine("Usage: B2SConverter <input-file> <output-file>")
        Console.WriteLine()
        Console.WriteLine("Supported formats:")
        Console.WriteLine("  .directb2s  - XML format with base64 embedded images")
        Console.WriteLine("  .zipb2s     - ZIP format with XML and separate image files")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  B2SConverter mybackglass.directb2s mybackglass.zipb2s")
        Console.WriteLine("  B2SConverter mybackglass.zipb2s mybackglass.directb2s")
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

