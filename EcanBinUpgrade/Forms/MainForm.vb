Imports System.ComponentModel
Imports System.IO
Imports System.Media
Imports System.Text
Imports Newtonsoft.Json
Imports Wangk.Hash
Imports Wangk.Resource

Public Class MainForm
    ''' <summary>
    ''' 发送间隔(ms)
    ''' </summary>
    Const SLEEPMS = 1

#Region "窗口"
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
#Region "初始化资源"
        LoadLocationSetting()

        With AppSetting
            '日志
            .logger = New Wangk.Tools.Logger With {
                .writeLevel = Wangk.Tools.LoggerHelper.Log.writeLevel.Level_DEBUG,
                .saveDaysMax = 30
            }
            .logger.Init()

            '.logger.LogThis("程序启动")
        End With
#End Region

#Region "样式设置"
        Dim assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location
        With My.Application.Info
            Me.Text = $"{ .ProductName} V{System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion}"
        End With

        SetLinkControlState(False)

        With ComboBoxItem1
            .Items.AddRange({"1000Kbps", "800Kbps", "666Kbps", "500Kbps", "400Kbps", "250Kbps", "200Kbps", "125Kbps", "100Kbps", "80Kbps", "50Kbps"})

            If ComboBoxItem1.Items.Contains(AppSetting.BPS) Then
                ComboBoxItem1.Text = AppSetting.BPS
            Else
                .SelectedIndex = 0
            End If
        End With

        TextBox1.Text = AppSetting.BinPath

        TabControl1.SelectedIndex = 1
#End Region
    End Sub

    Private Sub MainForm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ButtonItem2_Click(Nothing, Nothing)

        AppSetting.BPS = ComboBoxItem1.Text
        AppSetting.BinPath = TextBox1.Text

        SaveLocationSetting()
    End Sub
#End Region

#Region "设备"
#Region "连接控件状态"
    ''' <summary>
    ''' 连接控件状态
    ''' </summary>
    Private Sub SetLinkControlState(link As Boolean)
        ButtonItem1.Enabled = Not link
        ButtonItem2.Enabled = link
        ButtonItem3.Enabled = link
        TabControl1.Enabled = link
        ComboBoxItem1.Enabled = Not link

        If link Then
            AutoReadTimer = New Threading.Timer(AddressOf ReadMessage, New Threading.AutoResetEvent(True), 0, 200)
        ElseIf AutoReadTimer IsNot Nothing Then
            AutoReadTimer.Dispose()
            AutoReadTimer = Nothing
        End If
    End Sub
#End Region

#Region "连接"
    Private Sub ButtonItem1_Click(sender As Object, e As EventArgs) Handles ButtonItem1.Click

    End Sub

    Private Sub ButtonItem1_MouseUp(sender As Object, e As MouseEventArgs) Handles ButtonItem1.MouseUp
        '查找设备
        If Ecan.OpenDevice(1, 0, 0) <> Ecan.ECANStatus.STATUS_OK Then
            MsgBox("未找到设备", MsgBoxStyle.Information, "连接")
            ToolStripStatusLabel1.Text = "未找到设备"
            Exit Sub
        End If

        Dim init_config As New Ecan.INIT_CONFIG() With {
            .AccCode = 0,
            .AccMask = &HFFFFFF,
            .Filter = 0,
            .Mode = 0
        }

        '波特率参数
        Dim tmpArray As Byte(,) = {
            {&H0, &H14},
            {&H0, &H16},
            {&H80, &HB6},
            {&H0, &H1C},
            {&H80, &HFA},
            {&H1, &H1C},
            {&H81, &HFA},
            {&H3, &H1C},
            {&H4, &H1C},
            {&H83, &HFF},
            {&H9, &H1C}}

        init_config.Timing0 = tmpArray(ComboBoxItem1.SelectedIndex, 0)
        init_config.Timing1 = tmpArray(ComboBoxItem1.SelectedIndex, 1)

        '初始化CAN
        If Ecan.InitCAN(1, 0, 0, init_config) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            Ecan.CloseDevice(1, 0)
            MsgBox("初始化CAN失败", MsgBoxStyle.Information, "连接")
            ToolStripStatusLabel1.Text = "初始化CAN失败"
            Exit Sub
        End If

        '启动CAN
        If Ecan.StartCAN(1, 0, 0) <> Ecan.ECANStatus.STATUS_OK Then
            Ecan.CloseDevice(1, 0)
            MsgBox("启动CAN失败", MsgBoxStyle.Information, "连接")
            ToolStripStatusLabel1.Text = "启动CAN失败"
            Exit Sub
        End If

        ToolStripStatusLabel1.Text = $"设备已连接"
        SetLinkControlState(True)
    End Sub
#End Region

#Region "断开"
    Private Sub ButtonItem2_Click(sender As Object, e As EventArgs) Handles ButtonItem2.Click
        SetLinkControlState(False)

        Try
            Ecan.CloseDevice(1, 0)
        Catch ex As Exception
        End Try

        ToolStripStatusLabel1.Text = "设备已断开"
    End Sub
#End Region

#Region "复位重启"
    Private Sub ButtonItem3_Click(sender As Object, e As EventArgs) Handles ButtonItem3.Click
        If Ecan.EcanDll.ResetCAN(1, 0, 0) <> Ecan.ECANStatus.STATUS_OK Then
            Exit Sub
        End If

        MsgBox("指令发送成功", MsgBoxStyle.Information, "复位重启")
    End Sub
#End Region
#End Region

#Region "单指令调试"
    ''' <summary>
    ''' 自动读回读标记
    ''' </summary>
    Private AutoReadMessageFlag As Boolean = True

    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl1.SelectedIndexChanged
        '单指令调试才自动回读
        AutoReadMessageFlag = If(TabControl1.SelectedIndex = 0, True, False)
    End Sub

    ''' <summary>
    ''' 自动读取计时器
    ''' </summary>
    Private AutoReadTimer As Threading.Timer

    ''' <summary>
    ''' 回读消息
    ''' </summary>
    Private Sub ReadMessage(sender As Object)
        If Not AutoReadMessageFlag Then
            Exit Sub
        End If

        Dim recMsg As New Ecan.CAN_OBJ
        Dim recMsgLength As UInt32 = 1

        '未收到消息
        If Ecan.EcanDll.Receive(1, 0, 0, recMsg, recMsgLength, 200) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            'ShowMessage($"Read> Fail")
            Exit Sub
        End If

        '消息数据长度为0
        If recMsgLength <= 0 Then
            ShowMessage($"Read> Count = 0")
            Exit Sub
        End If

        ShowMessage($"Read> ID:{recMsg.ID.ToString("X")} Data:{BitConverter.ToString(recMsg.data).ToLower}")
    End Sub

    Public Delegate Sub ShowMessageCallback(value As String)
    ''' <summary>
    ''' 显示回读消息
    ''' </summary>
    Public Sub ShowMessage(value As String)
        If Me.InvokeRequired Then
            Me.Invoke(New ShowMessageCallback(AddressOf ShowMessage),
                      New Object() {value})
            Exit Sub
        End If

        TextBox3.AppendText(value & vbCrLf)
    End Sub

    '发送数据
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If TextBox2.Text = "" Then
            Exit Sub
        End If

        Dim sendMsg As New Ecan.CAN_OBJ
        With sendMsg
            .ID = Convert.ToUInt32(AppSetting.MessageID, 16)
            .ExternFlag = If(CheckBox1.Checked, 1, 0)
            .RemoteFlag = If(CheckBox2.Checked, 1, 0)

            ReDim .data(8 - 1)
            Dim tmpData = Wangk.Hash.Hex2Bin(TextBox2.Text)
            For i001 = 0 To tmpData.Length - 1
                .data(i001) = tmpData(i001)
            Next
            .DataLen = tmpData.Length

            ReDim .Reserved(3 - 1)

            .SendType = 0
        End With

        If Ecan.EcanDll.Transmit(1, 0, 0, sendMsg, 1) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            MsgBox("发送失败", MsgBoxStyle.Information, "发送")
        End If

        TextBox3.AppendText($"Send> ID:{sendMsg.ID.ToString("X")} Data:{BitConverter.ToString(sendMsg.data).ToLower}{vbCrLf}")
    End Sub

    '清空记录
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        TextBox3.Clear()
    End Sub
#End Region

#Region "单片机升级"
#Region "选择文件"
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim TmpDialog As New OpenFileDialog With {
            .Filter = "升级文件|*.hex"
        }
        If TmpDialog.ShowDialog() <> DialogResult.OK Then
            Exit Sub
        End If

        TextBox1.Text = TmpDialog.FileName

    End Sub
#End Region

#Region "升级"
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
#Region "参数检查"
        If TextBox1.Text = "" Then
            MsgBox($"未选择文件", MsgBoxStyle.Information, "升级")
            Exit Sub
        End If
#End Region

        Dim testTime As New Stopwatch
        testTime.Start()

        Dim tmpDialog = New BackgroundWorkDialog With {
                        .Text = "发送升级程序"
                    }

        tmpDialog.Start(AddressOf UpgradeWork, TextBox1.Text)

        If tmpDialog.Error IsNot Nothing Then

            AppSetting.logger.LogThis(tmpDialog.Error.ToString)

            MsgBox(tmpDialog.Error.Message,
                   MsgBoxStyle.Critical,
                   "发送失败")
            Exit Sub

        ElseIf tmpDialog.IsCancel Then
            MsgBox("发送已取消",
                   MsgBoxStyle.Critical,
                   "发送升级程序")
            Exit Sub

        End If

        Dim tmpSP As SoundPlayer = New SoundPlayer(".\Resource\warning.wav")
        tmpSP.Play()

        MsgBox($"发送完毕,耗时 {testTime.ElapsedMilliseconds \ 1000} s",
               MsgBoxStyle.MsgBoxSetForeground,
               "发送升级程序")

    End Sub

    ''' <summary>
    ''' 升级函数
    ''' </summary>
    Private Sub UpgradeWork(e As BackgroundWorkEventArgs)
        Dim FilesPath As String = e.Args

        e.Write("获取程序入口地址")
        Dim programEntryAddress() As Byte = GetProgramEntryAddress(FilesPath)

#Region "单片机复位"
        e.Write("等待单片机复位(10s)")
        'Ecan.EcanDll.ResetCAN(1, 0, 0)

        If Not CheckReceiveMsg(10, {&HDD, &HDD}) Then
            Throw New Exception("接收单片机复位回复失败")
        End If
#End Region

#Region "发送升级指令"
        e.Write("发送升级指令")
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ(Hex2Bin("08aa")), 1) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            Throw New Exception("发送升级指令失败")
        End If

        If Not CheckReceiveMsg(10, {&HAA, &HAA}) Then
            Throw New Exception("接收升级指令回复失败")
        End If
#End Region

#Region "下发保留字"
        e.Write("下发保留字")
        For i001 = 0 To 8 - 1
            If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ(Hex2Bin("0000")), 1) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
                Throw New Exception($"发送第{i001 + 1}保留字失败")
            End If
        Next
#End Region

#Region "下发程序入口地址"
        e.Write("下发程序入口地址")
        '高字
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ({programEntryAddress(0), programEntryAddress(1)}), 1) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            Throw New Exception("发送程序入口高地址失败")
        End If
        Threading.Thread.Sleep(SLEEPMS)

        '低字
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ({programEntryAddress(2), programEntryAddress(3)}), 1) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
            Throw New Exception("发送程序入口低地址失败")
        End If
        Threading.Thread.Sleep(SLEEPMS)
#End Region

#Region "发送HEX文件(合并发送)"
        e.Write("发送HEX文件")
        Dim RowCount = GetFileRowCount(FilesPath)
        Dim RID = 0

        Using reader As New StreamReader(FilesPath, Encoding.UTF8)

            Dim writeAddress(4 - 1) As Byte
            Dim dataList As New List(Of Byte())
            Dim getLowerAddressFlag As Boolean = False

            Do
                RID += 1

                If e.IsCancel Then
                    Exit Sub
                End If

                Dim tmpStr = reader.ReadLine()
                If tmpStr Is Nothing Then
                    Exit Do
                End If

                Dim tmpData = Hex2Bin(tmpStr.Replace(":", ""))

                If tmpData(0) = &H2 AndAlso
                    tmpData(3) = &H4 Then
                    '切换高地址

                    SendHexCacheData(writeAddress, dataList)

                    '更新HighAddress
                    writeAddress(0) = tmpData(4)
                    writeAddress(1) = tmpData(5)
                    getLowerAddressFlag = True

                ElseIf (tmpData(1) = &H7F AndAlso tmpData(2) = &HF6) OrElse
                        (tmpData(1) = &H7F AndAlso tmpData(2) = &HF8) Then
                    '跳过保留地址

                    SendHexCacheData(writeAddress, dataList)

                Else
                    '正常数据

                    '更新LowerAddress
                    If getLowerAddressFlag = True Then

                        getLowerAddressFlag = False
                        writeAddress(2) = tmpData(1)
                        writeAddress(3) = tmpData(2)
                    End If

                    Dim progressCount = RID / RowCount

                    '合并数据
                    For i001 = 0 To tmpData(0) - 1 Step 2
                        dataList.Add({tmpData(4 + i001), tmpData(4 + 1 + i001)})
                    Next

                    e.Write(CInt(progressCount * 100))

                    If tmpData(0) = 0 Then
                        '文件结尾
                        SendHexCacheData(writeAddress, dataList)
                        SendHexData(writeAddress, dataList)
                        Exit Do

                    ElseIf tmpData(0) <> &H20 Then
                        '后续地址不连续
                        SendHexCacheData(writeAddress, dataList)
                        getLowerAddressFlag = True

                    ElseIf dataList.Count > 128 - 16 Then
                        '超过指定字Word个数则发送
                        SendHexCacheData(writeAddress, dataList)
                        getLowerAddressFlag = True

                    End If

                End If

            Loop
        End Using
#End Region

        e.Write($"接收单片机最后回复")
        If Not CheckReceiveMsg(10, {&H0, &HBB}) Then
            Throw New Exception("接收单片机最后回复失败")
        End If

    End Sub

#Region "发送Hex文件数据"
    ''' <summary>
    ''' 发送缓存的Hex文件数据
    ''' </summary>
    Private Sub SendHexCacheData(address As Byte(), dataList As List(Of Byte()))
        If dataList.Count > 0 Then
            SendHexData(address, dataList)
        End If

    End Sub

    ''' <summary>
    ''' 发送Hex文件数据
    ''' </summary>
    Private Sub SendHexData(address As Byte(), dataList As List(Of Byte()))
        '数据长度
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ({dataList.Count \ &H100, dataList.Count Mod &H100}), 1) <>
            Ecan.EcanDll.ECANStatus.STATUS_OK Then

            Throw New Exception($"发送数据长度失败")
        End If
        Threading.Thread.Sleep(SLEEPMS)

        If dataList.Count = 0 Then
            Exit Sub
        End If

        '进行CRC校验的数据
        Dim tmpCRCDataArray((4 + dataList.Count * 2) - 1) As Byte
        Array.Copy(address, tmpCRCDataArray, 4)

        '高地址
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ({address(0), address(1)}), 1) <>
            Ecan.EcanDll.ECANStatus.STATUS_OK Then

            Throw New Exception($"发送数据高地址失败")
        End If
        Threading.Thread.Sleep(SLEEPMS)

        '低地址
        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ({address(2), address(3)}), 1) <>
            Ecan.EcanDll.ECANStatus.STATUS_OK Then

            Throw New Exception($"发送数据低地址失败")
        End If
        Threading.Thread.Sleep(SLEEPMS)

        '数据
        For i001 = 0 To dataList.Count - 1

            tmpCRCDataArray(4 + i001 * 2) = dataList(i001)(0)
            tmpCRCDataArray(4 + i001 * 2 + 1) = dataList(i001)(1)

            If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ(dataList(i001)), 1) <>
                Ecan.EcanDll.ECANStatus.STATUS_OK Then

                Throw New Exception($"发送数据 失败")
            End If

            If Not CheckReceiveMsg(2, {&H0, &HBB}) Then
                Throw New Exception($"单片机接收数据 失败")
            End If
        Next

        'CRC校验及写入完成
        Dim tmpCRCCode As UShort = Wangk.Hash.GetCRC16Modbus(tmpCRCDataArray)
        '转换后低字节在前,高字节在后
        Dim tmpCRCCodeBytes = BitConverter.GetBytes(tmpCRCCode)

        If Ecan.EcanDll.Transmit(1, 0, 0, CreateCAN_OBJ(tmpCRCCodeBytes), 1) <>
                Ecan.EcanDll.ECANStatus.STATUS_OK Then

            Throw New Exception($"发送CRC校验 失败")
        End If

        If Not CheckReceiveMsg(5, {&H0, &HBB}) Then
            Throw New Exception($"接收单片机CRC回复 失败")
        End If

        dataList.Clear()

    End Sub
#End Region

#Region "校验接收的数据"
    ''' <summary>
    ''' 校验接收的数据
    ''' </summary>
    Private Function CheckReceiveMsg(WaitSec As Integer, checkData As Byte()) As Boolean

        If checkData.Length > 8 Then
            Throw New Exception("对比数据长度不能超过8字节")
        End If

        Dim ReadCount = 0
        Do While ReadCount < WaitSec * 5
            ReadCount += 1

            Dim recMsg As New Ecan.CAN_OBJ
            Dim recMsgLength As UInt32 = 1

            '未收到消息
            If Ecan.EcanDll.Receive(1, 0, 0, recMsg, recMsgLength, 200) <> Ecan.EcanDll.ECANStatus.STATUS_OK Then
                Continue Do
            End If

            '消息数据长度为0
            If recMsgLength <= 0 Then
                Continue Do
            End If

            For i001 = 0 To checkData.Length - 1
                If checkData(i001) <> recMsg.data(i001) Then
                    Continue Do
                End If
            Next

            Return True
        Loop

        Return False
    End Function
#End Region

#Region "创建发送帧"
    ''' <summary>
    ''' 创建发送帧
    ''' </summary>
    Private Function CreateCAN_OBJ(data As Byte()) As Ecan.CAN_OBJ
        Dim sendMsg As New Ecan.CAN_OBJ
        With sendMsg
            .ID = Convert.ToUInt32(AppSetting.MessageID, 16)
            .ExternFlag = 1
            .RemoteFlag = 0

            ReDim .data(8 - 1)
            For i001 = 0 To 8 - 1
                If data.Length <= i001 Then
                    Exit For
                End If

                .data(i001) = data(i001)
            Next
            .DataLen = If(data.Length <= 8, data.Length, 8)

            ReDim .Reserved(3 - 1)

            .SendType = 0
        End With
        Return sendMsg
    End Function
#End Region

#End Region
#End Region
End Class