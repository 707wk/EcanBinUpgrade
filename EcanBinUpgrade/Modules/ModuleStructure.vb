Imports System.Xml.Serialization
Imports Newtonsoft

Public Module ModuleStructure
#Region "系统配置"
    ''' <summary>
    ''' 系统配置
    ''' </summary>
    Public Structure SystemSetting
#Region "运行参数"
        ''' <summary>
        ''' 波特率
        ''' </summary>
        Dim BPS As String
        ''' <summary>
        ''' 升级文件路径
        ''' </summary>
        Dim BinPath As String

        ''' <summary>
        ''' 报文帧ID
        ''' </summary>
        Dim MessageID As String
#End Region

        ''' <summary>
        ''' 日志记录
        ''' </summary>
        <Json.JsonIgnore>
        Dim logger As Wangk.Tools.Logger

    End Structure
#End Region
End Module
