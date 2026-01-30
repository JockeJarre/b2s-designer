Imports System.IO
Imports System.IO.Compression
Imports System.Xml
Imports FreeImageAPI

''' <summary>
''' Represents a B2S backglass file (directb2s or B2Sz format)
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
                Case ".b2sz"
                    Return LoadB2Sz(filePath)
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
        ''' Load a B2Sz file (ZIP format with XML and separate image files)
        ''' </summary>
        Public Shared Function LoadB2Sz(filePath As String) As B2SFile
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
                    Throw New InvalidDataException("No XML file found in B2Sz archive")
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
        ''' Save to B2Sz format (ZIP with XML and separate image files)
        ''' </summary>
        Public Sub SaveB2Sz(filePath As String)
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

            ' This is used when converting from B2Sz back to directb2s
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
                                fileNameAttr = xmlDoc.CreateAttribute("FileName")
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
                                fileNameAttr = xmlDoc.CreateAttribute("FileName")
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

            Try
                ' Use FreeImage to detect format automatically
                Using ms As New MemoryStream(imageBytes)
                    Dim imageType = FreeImage.GetFileTypeFromStream(ms)
                    
                    If imageType <> FREE_IMAGE_FORMAT.FIF_UNKNOWN Then
                        ' Map FreeImage format to file extension
                        Select Case imageType
                            Case FREE_IMAGE_FORMAT.FIF_PNG
                                Return "png"
                            Case FREE_IMAGE_FORMAT.FIF_JPEG
                                Return "jpg"
                            Case FREE_IMAGE_FORMAT.FIF_GIF
                                Return "gif"
                            Case FREE_IMAGE_FORMAT.FIF_BMP
                                Return "bmp"
                            Case FREE_IMAGE_FORMAT.FIF_WEBP
                                Return "webp"
                            Case FREE_IMAGE_FORMAT.FIF_TIFF
                                Return "tiff"
                            Case FREE_IMAGE_FORMAT.FIF_TARGA
                                Return "tga"
                            Case FREE_IMAGE_FORMAT.FIF_ICO
                                Return "ico"
                            Case FREE_IMAGE_FORMAT.FIF_PSD
                                Return "psd"
                            Case FREE_IMAGE_FORMAT.FIF_EXR
                                Return "exr"
                            Case FREE_IMAGE_FORMAT.FIF_J2K, FREE_IMAGE_FORMAT.FIF_JP2
                                Return "jp2"
                            Case FREE_IMAGE_FORMAT.FIF_JXR
                                Return "jxr"
                            Case Else
                                ' For any other supported format, try to get extension from FreeImage
                                Dim ext = FreeImage.GetFIFExtensionList(imageType)
                                If Not String.IsNullOrEmpty(ext) Then
                                    ' Get first extension from comma-separated list
                                    Dim extensions = ext.Split(","c)
                                    If extensions.Length > 0 Then
                                        Return extensions(0).Trim()
                                    End If
                                End If
                        End Select
                    End If
                End Using
            Catch ex As Exception
                ' If FreeImage detection fails, fall back to manual detection
            End Try

            ' Fallback: Manual signature detection for common formats
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

            ' Check for WEBP signature (RIFF....WEBP)
            If imageBytes.Length >= 12 AndAlso 
               imageBytes(0) = &H52 AndAlso imageBytes(1) = &H49 AndAlso imageBytes(2) = &H46 AndAlso imageBytes(3) = &H46 AndAlso
               imageBytes(8) = &H57 AndAlso imageBytes(9) = &H45 AndAlso imageBytes(10) = &H42 AndAlso imageBytes(11) = &H50 Then
                Return "webp"
            End If

            ' Check for TIFF signature (II or MM)
            If imageBytes.Length >= 4 AndAlso 
               ((imageBytes(0) = &H49 AndAlso imageBytes(1) = &H49 AndAlso imageBytes(2) = &H2A AndAlso imageBytes(3) = &H00) OrElse
                (imageBytes(0) = &H4D AndAlso imageBytes(1) = &H4D AndAlso imageBytes(2) = &H00 AndAlso imageBytes(3) = &H2A)) Then
                Return "tiff"
            End If

            ' Default to PNG
            Return "png"
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
