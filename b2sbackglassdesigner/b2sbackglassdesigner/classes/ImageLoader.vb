' Helper class for loading image files including AVIF format
' This uses ImageSharp library for AVIF support and converts to System.Drawing.Bitmap
Imports System.Drawing
Imports System.IO

Public Class ImageLoader

    ''' <summary>
    ''' Determines if ImageSharp is available for loading images
    ''' </summary>
    Private Shared _imageSharpAvailable As Boolean? = Nothing

    ''' <summary>
    ''' Check if ImageSharp library is available
    ''' </summary>
    Private Shared Function IsImageSharpAvailable() As Boolean
        If _imageSharpAvailable.HasValue Then
            Return _imageSharpAvailable.Value
        End If

        Try
            ' Try to load the ImageSharp assembly
            Dim assembly = System.Reflection.Assembly.Load("SixLabors.ImageSharp")
            _imageSharpAvailable = (assembly IsNot Nothing)
        Catch
            _imageSharpAvailable = False
        End Try

        Return _imageSharpAvailable.Value
    End Function

    ''' <summary>
    ''' Load an image file, supporting standard formats plus AVIF if ImageSharp is available
    ''' </summary>
    Public Shared Function LoadImage(filePath As String) As Image
        If Not File.Exists(filePath) Then
            Throw New FileNotFoundException("Image file not found", filePath)
        End If

        Dim extension = Path.GetExtension(filePath).ToLower()

        ' For AVIF, try to use ImageSharp
        If extension = ".avif" Then
            If IsImageSharpAvailable() Then
                Return LoadAvifImage(filePath)
            Else
                ' ImageSharp not available - throw informative error
                Throw New NotSupportedException("AVIF format requires ImageSharp library. Please add SixLabors.ImageSharp NuGet package to the project.")
            End If
        End If

        ' For all other formats, use built-in .NET Image loading
        Return Bitmap.FromFile(filePath)
    End Function

    ''' <summary>
    ''' Load an AVIF image file using ImageSharp
    ''' </summary>
    Private Shared Function LoadAvifImage(filePath As String) As Image
        If Not IsImageSharpAvailable() Then
            Throw New NotSupportedException("AVIF format is not supported. ImageSharp library is not available.")
        End If

        Try
            ' Use reflection to call ImageSharp since we can't add it as a direct reference
            ' in the old-style VB project format
            Dim imageSharpAssembly = System.Reflection.Assembly.Load("SixLabors.ImageSharp")
            Dim imageType = imageSharpAssembly.GetType("SixLabors.ImageSharp.Image")
            
            ' Call Image.Load(string path)
            Dim loadMethod = imageType.GetMethod("Load", New Type() {GetType(String)})
            If loadMethod Is Nothing Then
                Throw New NotSupportedException("ImageSharp Load method not found")
            End If

            Dim imageSharpImage = loadMethod.Invoke(Nothing, New Object() {filePath})
            
            ' Convert ImageSharp image to System.Drawing.Bitmap
            Return ConvertImageSharpToBitmap(imageSharpImage)
        Catch ex As Exception
            Throw New Exception($"Failed to load AVIF image: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Convert ImageSharp image to System.Drawing.Bitmap
    ''' </summary>
    Private Shared Function ConvertImageSharpToBitmap(imageSharpImage As Object) As Bitmap
        Try
            ' Get the image dimensions
            Dim widthProp = imageSharpImage.GetType().GetProperty("Width")
            Dim heightProp = imageSharpImage.GetType().GetProperty("Height")
            Dim width = CInt(widthProp.GetValue(imageSharpImage))
            Dim height = CInt(heightProp.GetValue(imageSharpImage))

            ' Save to memory stream as PNG
            Dim ms As New MemoryStream()
            
            ' Get the SaveAsPng method
            Dim saveMethod = imageSharpImage.GetType().GetMethod("SaveAsPng", New Type() {GetType(Stream)})
            If saveMethod Is Nothing Then
                Throw New NotSupportedException("ImageSharp SaveAsPng method not found")
            End If
            
            saveMethod.Invoke(imageSharpImage, New Object() {ms})
            ms.Position = 0
            
            ' Load as Bitmap - create a copy so we can dispose the stream
            Dim tempBitmap = New Bitmap(ms)
            Dim bitmap = New Bitmap(tempBitmap)
            tempBitmap.Dispose()
            ms.Dispose()
            
            ' Dispose the ImageSharp image
            If TypeOf imageSharpImage Is IDisposable Then
                DirectCast(imageSharpImage, IDisposable).Dispose()
            End If
            
            Return bitmap
        Catch ex As Exception
            Throw New Exception($"Failed to convert ImageSharp image to Bitmap: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Copy an image to avoid file locking issues
    ''' </summary>
    Public Shared Function CopyImage(image As Image) As Image
        If image Is Nothing Then
            Return Nothing
        End If

        Dim ms As New MemoryStream()
        Try
            image.Save(ms, image.RawFormat)
            ms.Position = 0
            Dim tempBitmap = New Bitmap(ms)
            Dim copy = New Bitmap(tempBitmap)
            tempBitmap.Dispose()
            Return copy
        Finally
            ms.Dispose()
        End Try
    End Function

End Class
