Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization
Imports Newtonsoft.Json


Module ModuleFunction
#Region "读取本地配置"
    ''' <summary>
    ''' 读取本地配置
    ''' </summary>
    Public Function LoadLocationSetting() As Boolean
        Dim Path As String = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech")
        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech\{My.Application.Info.Title}")
        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech\{My.Application.Info.Title}\Data")

        '反序列化
        Try
            AppSetting = JsonConvert.DeserializeObject(Of SystemSetting)(
                System.IO.File.ReadAllText($"{Path}\Hunan Yestech\{My.Application.Info.Title}\Data\Setting.json",
                                           System.Text.Encoding.UTF8))
        Catch exF As Exception
            '使用默认参数
            AppSetting.BPS = ""
            AppSetting.BinPath = ""
            AppSetting.MessageID = "9AAAAABB"
        End Try

        '兼容旧版本
        If AppSetting.MessageID Is Nothing Then
            AppSetting.MessageID = "9AAAAABB"
        End If

        Return True
    End Function
#End Region

#Region "保存本地配置"
    ''' <summary>
    ''' 保存本地配置
    ''' </summary>
    Public Function SaveLocationSetting() As Boolean
        Dim Path As String = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech")
        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech\{My.Application.Info.Title}")
        System.IO.Directory.CreateDirectory($"{Path}\Hunan Yestech\{My.Application.Info.Title}\Data")

        '序列化
        Try
            Using t As System.IO.StreamWriter = New System.IO.StreamWriter(
                $"{Path}\Hunan Yestech\{My.Application.Info.Title}\Data\Setting.json",
                False,
                System.Text.Encoding.UTF8)

                t.Write(JsonConvert.SerializeObject(AppSetting))
            End Using

        Catch ex As Exception
            MsgBox(ex.Message,
                   MsgBoxStyle.Information,
                   "保存配置异常")
            Return False
        End Try

        Return True
    End Function
#End Region

#Region "获取文件行数"
    ''' <summary>
    ''' 获取文件行数
    ''' </summary>
    Public Function GetFileRowCount(path As String) As Integer
        Dim rowCount = 0

        Using reader As New StreamReader(path, Encoding.Default)
            Do While reader.ReadLine IsNot Nothing
                rowCount += 1
            Loop
        End Using

        Return rowCount
    End Function
#End Region

#Region "获取程序入口地址"
    ''' <summary>
    ''' 获取程序入口地址
    ''' </summary>
    Public Function GetProgramEntryAddress(path As String) As Byte()
        Dim addressData(4 - 1) As Byte

        Dim getHighAddressFlag As Boolean = False
        Using reader As New StreamReader(path, Encoding.Default)
            Dim HighAddress(2 - 1) As Byte
            Do
                Dim tmpStr = reader.ReadLine
                If tmpStr Is Nothing Then
                    Exit Do
                End If

                Dim tmpData = Wangk.Hash.Hex2Bin(tmpStr.Replace(":", ""))

                '判断类型
                If tmpData(0) = &H2 AndAlso
                    tmpData(3) = &H4 AndAlso
                    Not getHighAddressFlag Then
                    '获取高地址
                    addressData(0) = tmpData(4)
                    addressData(1) = tmpData(5)
                    getHighAddressFlag = True

                ElseIf (tmpData(1) = &H7F AndAlso tmpData(2) = &HF6) Then
                    '获取低地址
                    addressData(2) = tmpData(6)
                    addressData(3) = tmpData(7)
                End If
            Loop

        End Using

        Return addressData
    End Function
#End Region

End Module
