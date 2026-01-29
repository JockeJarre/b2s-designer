' Helper class for loading image files 
Imports System.Drawing
Imports System.IO

Public Class ImageLoader
    ''' <summary>
    ''' Load an image file, supporting standard formats
    ''' </summary>
    Public Shared Function LoadImage(filePath As String) As Image
        If Not File.Exists(filePath) Then
            Throw New FileNotFoundException("Image file not found", filePath)
        End If

        Dim extension = Path.GetExtension(filePath).ToLower()

        ' For all other formats, use built-in .NET Image loading
        Return Bitmap.FromFile(filePath)
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
