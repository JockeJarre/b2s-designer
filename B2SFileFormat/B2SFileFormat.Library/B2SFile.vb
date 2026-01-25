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
                Dim xmlEntry = archive.Entries.FirstOrDefault(Function(e) e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
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
                ' Add XML file
                Dim xmlEntryName = Path.GetFileNameWithoutExtension(filePath) & ".xml"
                Dim xmlEntry = archive.CreateEntry(xmlEntryName)
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

            ' Find all Image attributes with base64 data
            Dim imageNodes = XmlDocument.SelectNodes("//*[@Image]")
            If imageNodes Is Nothing Then Return

            Dim imageCounter = 0
            For Each node As XmlNode In imageNodes
                Dim imageAttr = node.Attributes("Image")
                If imageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(imageAttr.Value) Then
                    Try
                        Dim base64Data = imageAttr.Value
                        Dim imageBytes = Convert.FromBase64String(base64Data)

                        ' Generate filename from node information
                        Dim fileName = GetImageFileName(node, imageCounter)
                        imageCounter += 1

                        ' Store in dictionary if not already there
                        If Not Images.ContainsKey(fileName) Then
                            Images(fileName) = imageBytes
                        End If
                    Catch ex As Exception
                        ' Skip invalid base64 data
                    End Try
                End If
            Next
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
                    If Images.ContainsKey(imagePath) Then
                        imageData = Images(imagePath)
                    Else
                        ' Try just the filename
                        Dim fileName = Path.GetFileName(imagePath)
                        Dim matchingKey = Images.Keys.FirstOrDefault(Function(k) Path.GetFileName(k) = fileName)
                        If matchingKey IsNot Nothing Then
                            imageData = Images(matchingKey)
                        End If
                    End If

                    If imageData IsNot Nothing Then
                        ' Update or create Image attribute with base64 data
                        Dim imageAttr = node.Attributes("Image")
                        If imageAttr Is Nothing Then
                            imageAttr = XmlDocument.CreateAttribute("Image")
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
            Dim imageNodes = xmlDoc.SelectNodes("//*[@Image]")
            If imageNodes Is Nothing Then Return

            Dim imageCounter = 0
            For Each node As XmlNode In imageNodes
                Dim imageAttr = node.Attributes("Image")
                If imageAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(imageAttr.Value) Then
                    ' Generate filename
                    Dim fileName = GetImageFileName(node, imageCounter)
                    imageCounter += 1

                    ' Replace base64 data with filename reference
                    imageAttr.Value = ""

                    ' Update FileName attribute if it exists
                    Dim fileNameAttr = node.Attributes("FileName")
                    If fileNameAttr IsNot Nothing Then
                        fileNameAttr.Value = fileName
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Generate a filename for an image based on its XML node
        ''' </summary>
        Private Function GetImageFileName(node As XmlNode, counter As Integer) As String
            ' Try to get existing filename
            Dim fileNameAttr = node.Attributes("FileName")
            If fileNameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(fileNameAttr.Value) Then
                Dim existingName = Path.GetFileName(fileNameAttr.Value)
                If Not String.IsNullOrEmpty(existingName) Then
                    Return existingName
                End If
            End If

            ' Try to get name from ID or Name attribute
            Dim idAttr = node.Attributes("ID")
            Dim nameAttr = node.Attributes("Name")

            Dim baseName = "image"
            If nameAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(nameAttr.Value) Then
                baseName = MakeValidFileName(nameAttr.Value)
            ElseIf idAttr IsNot Nothing AndAlso Not String.IsNullOrEmpty(idAttr.Value) Then
                baseName = "id_" & idAttr.Value
            End If

            ' Add node name prefix
            Dim prefix = node.Name.ToLower()
            If prefix = "mainimage" Then
                baseName = "main_" & baseName
            ElseIf prefix = "bulb" Then
                baseName = "bulb_" & baseName
            End If

            ' Add counter to ensure uniqueness
            Return $"{baseName}_{counter}.png"
        End Function

        ''' <summary>
        ''' Make a string safe for use as a filename
        ''' </summary>
        Private Function MakeValidFileName(name As String) As String
            Dim invalid = Path.GetInvalidFileNameChars()
            Dim result = String.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            Return If(String.IsNullOrEmpty(result), "image", result)
        End Function
End Class
