Imports System.IO
Imports System.IO.Compression
Imports System.Xml

''' <summary>
''' Represents a B2S backglass file (directb2s or zipb2s format)
''' </summary>
Public Class B2SFile
    Public Property XmlDocument As XmlDocument
    Public Property Images As New Dictionary(Of String, Byte())

    Public Sub New()
        XmlDocument = New XmlDocument()
        End Sub

        ''' <summary>
        ''' Load a B2S file from disk (auto-detects format)
        ''' </summary>
        Public Shared Function Load(filePath As String) As B2SFile
            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException("File not found", filePath)
            End If

            Dim extension = Path.GetExtension(filePath).ToLower()
            Select Case extension
                Case ".directb2s"
                    Return LoadDirectB2S(filePath)
                Case ".zipb2s"
                    Return LoadZipB2S(filePath)
                Case Else
                    Throw New NotSupportedException($"Unsupported file extension: {extension}")
            End Select
        End Function

        ''' <summary>
        ''' Load a directb2s file (XML format with embedded base64 images)
        ''' </summary>
        Public Shared Function LoadDirectB2S(filePath As String) As B2SFile
            Dim result = New B2SFile()
            result.XmlDocument.Load(filePath)
            Return result
        End Function

        ''' <summary>
        ''' Load a zipb2s file (ZIP format with XML and separate image files)
        ''' </summary>
        Public Shared Function LoadZipB2S(filePath As String) As B2SFile
            Dim result = New B2SFile()

            Using archive = ZipFile.OpenRead(filePath)
                ' Find and load the main XML file
                ' First try to find "parameters.xml" (new standard name)
                Dim xmlEntry = archive.Entries.FirstOrDefault(Function(e) e.Name.Equals("parameters.xml", StringComparison.OrdinalIgnoreCase))
                
                ' If not found, fall back to any XML file (backwards compatibility)
                If xmlEntry Is Nothing Then
                    xmlEntry = archive.Entries.FirstOrDefault(Function(e) e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                End If

                If xmlEntry Is Nothing Then
                    Throw New InvalidDataException("No XML file found in zipb2s archive")
                End If

                Using xmlStream = xmlEntry.Open()
                    result.XmlDocument.Load(xmlStream)
                End Using

                ' Load all image files
                For Each entry In archive.Entries
                    If Not entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) AndAlso entry.Length > 0 Then
                        Using stream = entry.Open()
                            Using ms = New MemoryStream()
                                stream.CopyTo(ms)
                                result.Images(entry.FullName) = ms.ToArray()
                            End Using
                        End Using
                    End If
                Next
            End Using

            Return result
        End Function

        ''' <summary>
        ''' Save to directb2s format (XML with embedded base64 images)
        ''' </summary>
        Public Sub SaveDirectB2S(filePath As String)
            ' Extract images from XML base64 if needed
            ExtractImagesFromXml()

            ' Re-embed images as base64
            EmbedImagesToXml()

            ' Save XML
            XmlDocument.Save(filePath)
        End Sub

        ''' <summary>
        ''' Save to zipb2s format (ZIP with XML and separate image files)
        ''' </summary>
        Public Sub SaveZipB2S(filePath As String)
            ' Extract images from XML
            ExtractImagesFromXml()

            ' Create a working copy of XML for modification
            Dim xmlCopy = CType(XmlDocument.Clone(), XmlDocument)

            ' Replace base64 image data with filenames in XML copy
            ReplaceBase64WithFilenames(xmlCopy)

            ' Create ZIP file
            If File.Exists(filePath) Then
                File.Delete(filePath)
            End If

            Using archive = ZipFile.Open(filePath, ZipArchiveMode.Create)
                ' Add XML file with hardcoded name "parameters.xml"
                Dim xmlEntry = archive.CreateEntry("parameters.xml")
                Using entryStream = xmlEntry.Open()
                    xmlCopy.Save(entryStream)
                End Using

                ' Add image files
                For Each kvp In Images
                    Dim imageEntry = archive.CreateEntry(kvp.Key)
                    Using entryStream = imageEntry.Open()
                        entryStream.Write(kvp.Value, 0, kvp.Value.Length)
                    End Using
                Next
            End Using
        End Sub

        ''' <summary>
        ''' Extract base64 images from XML to Images dictionary
        ''' </summary>
        Private Sub ExtractImagesFromXml()
            If XmlDocument Is Nothing Then Return

            Dim imageCounter = 0

            ' Find all Image attributes with base64 data (for bulbs, reels, etc.)
            Dim imageNodes = XmlDocument.SelectNodes("//*[@Image]")
            If imageNodes IsNot Nothing Then
                For Each node As XmlNode In imageNodes
                    Dim imageAttr = node.Attributes("Image")
                    If imageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(imageAttr.Value) Then
                        Try
                            Dim base64Data = imageAttr.Value
                            Dim imageBytes = Convert.FromBase64String(base64Data)

                            ' Detect image format and generate filename with correct extension
                            Dim fileName = GetImageFileName(node, imageCounter, imageBytes)
                            imageCounter += 1

                            ' Store in dictionary if not already there
                            If Not Images.ContainsKey(fileName) Then
                                Images(fileName) = imageBytes
                            End If
                        Catch ex As Exception
                            ' Skip invalid base64 data
                        End Try
                    End If

                    ' Also check for OffImage attribute
                    Dim offImageAttr = node.Attributes("OffImage")
                    If offImageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(offImageAttr.Value) Then
                        Try
                            Dim base64Data = offImageAttr.Value
                            Dim imageBytes = Convert.FromBase64String(base64Data)

                            ' Create a temporary node for filename generation with "off" prefix
                            Dim tempNode = node.Clone()
                            Dim nameAttr = tempNode.Attributes("Name")
                            If nameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(nameAttr.Value) Then
                                nameAttr.Value = nameAttr.Value & "_off"
                            End If

                            Dim fileName = GetImageFileName(tempNode, imageCounter, imageBytes)
                            imageCounter += 1

                            If Not Images.ContainsKey(fileName) Then
                                Images(fileName) = imageBytes
                            End If
                        Catch ex As Exception
                            ' Skip invalid base64 data
                        End Try
                    End If
                Next
            End If

            ' Find all Value attributes with base64 data (for main images like BackglassImage, DMDImage, etc.)
            Dim valueNodes = XmlDocument.SelectNodes("//*[@Value]")
            If valueNodes IsNot Nothing Then
                For Each node As XmlNode In valueNodes
                    Dim valueAttr = node.Attributes("Value")
                    If valueAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(valueAttr.Value) Then
                        Try
                            Dim base64Data = valueAttr.Value
                            Dim imageBytes = Convert.FromBase64String(base64Data)

                            ' Detect image format and generate filename with correct extension
                            Dim fileName = GetImageFileName(node, imageCounter, imageBytes)
                            imageCounter += 1

                            ' Store in dictionary if not already there
                            If Not Images.ContainsKey(fileName) Then
                                Images(fileName) = imageBytes
                            End If
                        Catch ex As Exception
                            ' Skip invalid base64 data (Value might not be base64)
                        End Try
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Embed images from Images dictionary back to XML as base64
        ''' </summary>
        Private Sub EmbedImagesToXml()
            If Images.Count = 0 Then Return

            ' This is used when converting from zipb2s back to directb2s
            Dim imageNodes = XmlDocument.SelectNodes("//*[@FileName]")
            If imageNodes Is Nothing Then Return

            For Each node As XmlNode In imageNodes
                Dim fileNameAttr = node.Attributes("FileName")
                If fileNameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(fileNameAttr.Value) Then
                    ' Look for matching image in our dictionary
                    Dim imagePath = fileNameAttr.Value

                    ' Try different path variations
                    Dim imageData As Byte() = Nothing
                    Dim matchedKey As String = Nothing
                    If Images.ContainsKey(imagePath) Then
                        imageData = Images(imagePath)
                        matchedKey = imagePath
                    Else
                        ' Try just the filename
                        Dim fileName = Path.GetFileName(imagePath)
                        matchedKey = Images.Keys.FirstOrDefault(Function(k) Path.GetFileName(k) = fileName)
                        If matchedKey IsNot Nothing Then
                            imageData = Images(matchedKey)
                        End If
                    End If

                    If imageData IsNot Nothing Then
                        ' Convert AVIF to PNG for directb2s embedding
                        If matchedKey IsNot Nothing AndAlso Path.GetExtension(matchedKey).ToLower() = ".avif" Then
                            imageData = ConvertAvifToPng(imageData)
                        End If

                        ' Determine which attribute to use based on node type
                        ' Nodes like BackglassImage, DMDImage use "Value" attribute
                        ' Nodes like Bulb use "Image" attribute
                        Dim attributeName As String = If(UsesValueAttribute(node), "Value", "Image")

                        ' Update or create the appropriate attribute with base64 data
                        Dim imageAttr = node.Attributes(attributeName)
                        If imageAttr Is Nothing Then
                            imageAttr = XmlDocument.CreateAttribute(attributeName)
                            node.Attributes.Append(imageAttr)
                        End If
                        imageAttr.Value = Convert.ToBase64String(imageData, Base64FormattingOptions.InsertLineBreaks)
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Replace base64 image data with filenames in XML
        ''' </summary>
        Private Sub ReplaceBase64WithFilenames(xmlDoc As XmlDocument)
            Dim imageCounter = 0

            ' Process all Image attributes (for bulbs, reels, etc.)
            Dim imageNodes = xmlDoc.SelectNodes("//*[@Image]")
            If imageNodes IsNot Nothing Then
                For Each node As XmlNode In imageNodes
                    Dim imageAttr = node.Attributes("Image")
                    If imageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(imageAttr.Value) Then
                        Try
                            ' Get the image bytes from base64 to detect format
                            Dim imageBytes = Convert.FromBase64String(imageAttr.Value)
                            
                            ' Generate filename with correct extension
                            Dim fileName = GetImageFileName(node, imageCounter, imageBytes)
                            imageCounter += 1

                            ' Replace base64 data with filename reference
                            imageAttr.Value = ""

                            ' Set or update FileName attribute
                            Dim fileNameAttr = node.Attributes("FileName")
                            If fileNameAttr Is Nothing Then
                                fileNameAttr = XmlDocument.CreateAttribute("FileName")
                                node.Attributes.Append(fileNameAttr)
                            End If
                            fileNameAttr.Value = fileName
                        Catch ex As Exception
                            ' Skip invalid base64 data
                            imageCounter += 1
                        End Try
                    End If

                    ' Also process OffImage attribute
                    Dim offImageAttr = node.Attributes("OffImage")
                    If offImageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(offImageAttr.Value) Then
                        Try
                            Dim imageBytes = Convert.FromBase64String(offImageAttr.Value)
                            
                            ' Create a temporary node for filename generation with "off" prefix
                            Dim tempNode = node.Clone()
                            Dim nameAttr = tempNode.Attributes("Name")
                            If nameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(nameAttr.Value) Then
                                nameAttr.Value = nameAttr.Value & "_off"
                            End If

                            Dim fileName = GetImageFileName(tempNode, imageCounter, imageBytes)
                            imageCounter += 1

                            ' Clear the OffImage data
                            offImageAttr.Value = ""
                        Catch ex As Exception
                            ' Skip invalid base64 data
                            imageCounter += 1
                        End Try
                    End If
                Next
            End If

            ' Process all Value attributes (for main images like BackglassImage, DMDImage, etc.)
            Dim valueNodes = xmlDoc.SelectNodes("//*[@Value]")
            If valueNodes IsNot Nothing Then
                For Each node As XmlNode In valueNodes
                    Dim valueAttr = node.Attributes("Value")
                    If valueAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(valueAttr.Value) Then
                        Try
                            ' Get the image bytes from base64 to detect format
                            Dim imageBytes = Convert.FromBase64String(valueAttr.Value)
                            
                            ' Generate filename with correct extension
                            Dim fileName = GetImageFileName(node, imageCounter, imageBytes)
                            imageCounter += 1

                            ' Replace base64 data with filename reference
                            valueAttr.Value = ""

                            ' Set or update FileName attribute
                            Dim fileNameAttr = node.Attributes("FileName")
                            If fileNameAttr Is Nothing Then
                                fileNameAttr = XmlDocument.CreateAttribute("FileName")
                                node.Attributes.Append(fileNameAttr)
                            End If
                            fileNameAttr.Value = fileName
                        Catch ex As Exception
                            ' Skip invalid base64 data (Value might not be base64)
                            ' Don't increment counter since this might not be image data
                        End Try
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Generate a filename for an image based on its XML node and image data
        ''' </summary>
        Private Function GetImageFileName(node As XmlNode, counter As Integer, imageBytes As Byte()) As String
            ' Try to get existing filename
            Dim fileNameAttr = node.Attributes("FileName")
            If fileNameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(fileNameAttr.Value) Then
                Dim existingName = Path.GetFileName(fileNameAttr.Value)
                If Not String.IsNullOrEmpty(existingName) Then
                    Return existingName
                End If
            End If

            ' Detect image format from bytes
            Dim extension = DetectImageFormat(imageBytes)

            ' Try to get name from ID or Name attribute
            Dim idAttr = node.Attributes("ID")
            Dim nameAttr = node.Attributes("Name")

            Dim baseName = "image"
            If nameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(nameAttr.Value) Then
                baseName = MakeValidFileName(nameAttr.Value)
            ElseIf idAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(idAttr.Value) Then
                baseName = "id_" & idAttr.Value
            End If

            ' Add node name prefix for better identification
            Dim prefix = node.Name.ToLower()
            If prefix = "mainimage" Then
                baseName = "main_" & baseName
            ElseIf prefix = "bulb" Then
                baseName = "bulb_" & baseName
            ElseIf prefix = "backglassimage" Then
                baseName = "backglass"
            ElseIf prefix = "backglassonimage" Then
                baseName = "backglass_on"
            ElseIf prefix = "backglassoffimage" Then
                baseName = "backglass_off"
            ElseIf prefix = "dmdimage" Then
                baseName = "dmd"
            ElseIf prefix = "illuminationimage" Then
                baseName = "illumination"
            ElseIf prefix = "thumbnailimage" Then
                baseName = "thumbnail"
            ElseIf prefix = "image" AndAlso node.ParentNode IsNot Nothing AndAlso node.ParentNode.Name.ToLower() = "images" Then
                ' Reel images under Reels/Images
                baseName = "reel_" & baseName
            End If

            ' Add counter to ensure uniqueness
            Return $"{baseName}_{counter}.{extension}"
        End Function

        ''' <summary>
        ''' Make a string safe for use as a filename
        ''' </summary>
        Private Function MakeValidFileName(name As String) As String
            Dim invalid = Path.GetInvalidFileNameChars()
            Dim result = String.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            Return If(String.IsNullOrEmpty(result), "image", result)
        End Function

        ''' <summary>
        ''' Detect image format from byte array
        ''' </summary>
        Private Function DetectImageFormat(imageBytes As Byte()) As String
            If imageBytes Is Nothing OrElse imageBytes.Length < 12 Then
                Return "png"
            End If

            ' Check for PNG signature
            If imageBytes.Length >= 8 AndAlso imageBytes(0) = &H89 AndAlso imageBytes(1) = &H50 AndAlso imageBytes(2) = &H4E AndAlso imageBytes(3) = &H47 Then
                Return "png"
            End If

            ' Check for JPEG signature
            If imageBytes.Length >= 2 AndAlso imageBytes(0) = &HFF AndAlso imageBytes(1) = &HD8 Then
                Return "jpg"
            End If

            ' Check for GIF signature
            If imageBytes.Length >= 6 AndAlso imageBytes(0) = &H47 AndAlso imageBytes(1) = &H49 AndAlso imageBytes(2) = &H46 Then
                Return "gif"
            End If

            ' Check for BMP signature
            If imageBytes.Length >= 2 AndAlso imageBytes(0) = &H42 AndAlso imageBytes(1) = &H4D Then
                Return "bmp"
            End If

            ' Check for AVIF signature (ftyp box with 'avif' brand)
            If imageBytes.Length >= 12 Then
                ' AVIF files start with ftyp box, check for 'avif' or 'avis' brand
                Dim ftypPos = 4
                If imageBytes(ftypPos) = &H66 AndAlso imageBytes(ftypPos + 1) = &H74 AndAlso imageBytes(ftypPos + 2) = &H79 AndAlso imageBytes(ftypPos + 3) = &H70 Then
                    ' Check major brand
                    If imageBytes.Length >= 16 Then
                        Dim brandPos = 8
                        If (imageBytes(brandPos) = &H61 AndAlso imageBytes(brandPos + 1) = &H76 AndAlso imageBytes(brandPos + 2) = &H69 AndAlso imageBytes(brandPos + 3) = &H66) OrElse
                           (imageBytes(brandPos) = &H61 AndAlso imageBytes(brandPos + 1) = &H76 AndAlso imageBytes(brandPos + 2) = &H69 AndAlso imageBytes(brandPos + 3) = &H73) Then
                            Return "avif"
                        End If
                    End If
                End If
            End If

            ' Default to PNG
            Return "png"
        End Function

        ''' <summary>
        ''' Convert AVIF image bytes to PNG using ImageSharp
        ''' </summary>
        Private Function ConvertAvifToPng(avifBytes As Byte()) As Byte()
            Try
                ' Use ImageSharp to convert AVIF to PNG
                Dim imageSharpAssembly = System.Reflection.Assembly.Load("SixLabors.ImageSharp")
                Dim imageType = imageSharpAssembly.GetType("SixLabors.ImageSharp.Image")

                ' Load the AVIF image from bytes
                Dim loadMethod = imageType.GetMethod("Load", New Type() {GetType(Byte())})
                If loadMethod Is Nothing Then
                    ' Try stream-based loading
                    Dim ms = New MemoryStream(avifBytes)
                    loadMethod = imageType.GetMethod("Load", New Type() {GetType(Stream)})
                    If loadMethod IsNot Nothing Then
                        Dim imageSharpImage = loadMethod.Invoke(Nothing, New Object() {ms})
                        Return ConvertImageSharpToPngBytes(imageSharpImage)
                    End If
                Else
                    Dim imageSharpImage = loadMethod.Invoke(Nothing, New Object() {avifBytes})
                    Return ConvertImageSharpToPngBytes(imageSharpImage)
                End If
            Catch ex As Exception
                ' If conversion fails, return original bytes
                Console.WriteLine($"Warning: Failed to convert AVIF to PNG: {ex.Message}")
            End Try

            ' Return original bytes if conversion fails
            Return avifBytes
        End Function

        ''' <summary>
        ''' Convert PNG image bytes to AVIF using ImageSharp
        ''' </summary>
        Public Shared Function ConvertPngToAvif(pngBytes As Byte()) As Byte()
            Try
                ' Use ImageSharp to convert PNG to AVIF
                Dim imageSharpAssembly = System.Reflection.Assembly.Load("SixLabors.ImageSharp")
                Dim imageType = imageSharpAssembly.GetType("SixLabors.ImageSharp.Image")

                ' Load the PNG image from bytes
                Using ms = New MemoryStream(pngBytes)
                    Dim loadMethod = imageType.GetMethod("Load", New Type() {GetType(Stream)})
                    If loadMethod IsNot Nothing Then
                        Dim imageSharpImage = loadMethod.Invoke(Nothing, New Object() {ms})
                        Return ConvertImageSharpToAvifBytes(imageSharpImage)
                    End If
                End Using
            Catch ex As Exception
                Throw New Exception($"Failed to convert PNG to AVIF: {ex.Message}", ex)
            End Try

            ' Return original bytes if conversion fails
            Return pngBytes
        End Function

        ''' <summary>
        ''' Convert ImageSharp image to PNG bytes
        ''' </summary>
        Private Function ConvertImageSharpToPngBytes(imageSharpImage As Object) As Byte()
            Try
                Using ms = New MemoryStream()
                    ' Get the SaveAsPng method
                    Dim saveMethod = imageSharpImage.GetType().GetMethod("SaveAsPng", New Type() {GetType(Stream)})
                    If saveMethod IsNot Nothing Then
                        saveMethod.Invoke(imageSharpImage, New Object() {ms})
                        Return ms.ToArray()
                    End If
                End Using
            Finally
                ' Dispose the ImageSharp image
                If TypeOf imageSharpImage Is IDisposable Then
                    DirectCast(imageSharpImage, IDisposable).Dispose()
                End If
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Convert ImageSharp image to AVIF bytes
        ''' </summary>
        Private Shared Function ConvertImageSharpToAvifBytes(imageSharpImage As Object) As Byte()
            Try
                Using ms = New MemoryStream()
                    ' Get the encoder type
                    Dim encodersType = imageSharpImage.GetType().Assembly.GetType("SixLabors.ImageSharp.Formats.Avif.AvifEncoder")
                    If encodersType IsNot Nothing Then
                        Dim encoder = Activator.CreateInstance(encodersType)
                        
                        ' Get the Save method that takes a Stream and encoder
                        Dim saveMethod = imageSharpImage.GetType().GetMethod("Save", New Type() {GetType(Stream), encodersType})
                        If saveMethod IsNot Nothing Then
                            saveMethod.Invoke(imageSharpImage, New Object() {ms, encoder})
                            Return ms.ToArray()
                        End If
                    End If
                End Using
            Finally
                ' Dispose the ImageSharp image
                If TypeOf imageSharpImage Is IDisposable Then
                    DirectCast(imageSharpImage, IDisposable).Dispose()
                End If
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Determines if an XML node uses "Value" attribute for image data (vs "Image" attribute)
        ''' </summary>
        Private Shared Function UsesValueAttribute(node As XmlNode) As Boolean
            If node Is Nothing Then Return False
            
            Dim nodeName = node.Name.ToLower()
            
            ' Main image nodes use "Value" attribute
            If nodeName = "backglassimage" OrElse 
               nodeName = "backglassonimage" OrElse 
               nodeName = "backglassoffimage" OrElse
               nodeName = "dmdimage" OrElse 
               nodeName = "illuminationimage" OrElse 
               nodeName = "thumbnailimage" Then
                Return True
            End If
            
            ' Reel images under Reels/Images also use "Value" attribute
            If nodeName = "image" AndAlso node.ParentNode IsNot Nothing AndAlso 
               node.ParentNode.Name.ToLower() = "images" Then
                Return True
            End If
            
            ' Everything else (Bulb, etc.) uses "Image" attribute
            Return False
        End Function
End Class
