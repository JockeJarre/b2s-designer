' Helper class for loading image files using FreeImage library
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports FreeImageAPI

Public Class ImageLoader
    ''' <summary>
    ''' Load an image file using FreeImage, supporting extended formats like WEBP
    ''' Falls back to System.Drawing for basic formats
    ''' </summary>
    Public Shared Function LoadImage(filePath As String) As Image
        If Not File.Exists(filePath) Then
            Throw New FileNotFoundException("Image file not found", filePath)
        End If

        Try
            ' Use FreeImage to load the image, which supports many more formats
            Dim dib As FIBITMAP = FreeImage.LoadEx(filePath)
            
            If dib.IsNull Then
                ' If FreeImage can't load it, try System.Drawing as fallback
                Return Bitmap.FromFile(filePath)
            End If
            
            Try
                ' Convert FreeImage bitmap to System.Drawing.Bitmap
                Dim bitmap As Bitmap = FreeImage.GetBitmap(dib)
                ' Create a copy to ensure we can dispose the FreeImage bitmap
                Dim result = New Bitmap(bitmap)
                Return result
            Finally
                ' Unload the FreeImage bitmap to free memory
                FreeImage.UnloadEx(dib)
            End Try
        Catch ex As Exception
            ' If FreeImage fails, fall back to standard .NET loading
            Try
                Return Bitmap.FromFile(filePath)
            Catch
                ' Re-throw the original exception if both methods fail
                Throw ex
            End Try
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

    ''' <summary>
    ''' Detect if a file is a supported image format using FreeImage
    ''' </summary>
    Public Shared Function IsSupportedFormat(filePath As String) As Boolean
        Try
            Dim imageType = FreeImage.GetFileType(filePath, 0)
            Return imageType <> FREE_IMAGE_FORMAT.FIF_UNKNOWN
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Get the format name of an image file
    ''' </summary>
    Public Shared Function GetImageFormatName(filePath As String) As String
        Try
            Dim imageType = FreeImage.GetFileType(filePath, 0)
            If imageType <> FREE_IMAGE_FORMAT.FIF_UNKNOWN Then
                Return FreeImage.GetFormatFromFIF(imageType)
            End If
        Catch
        End Try
        Return "Unknown"
    End Function

End Class
