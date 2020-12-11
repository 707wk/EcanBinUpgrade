Imports System.Runtime.InteropServices

Namespace Ecan
    Public Module EcanDll
        ''' <summary>
        ''' 执行结果
        ''' </summary>
        <Flags()>
        Public Enum ECANStatus As UInteger
            ''' <summary>
            ''' 失败
            ''' </summary>
            STATUS_ERR = &H0
            ''' <summary>
            ''' 成功
            ''' </summary>
            STATUS_OK = &H1
        End Enum

        ''' <summary>
        ''' CAN信息帧,用于Transmit和Receive
        ''' </summary>
        Public Structure CAN_OBJ
            ''' <summary>
            ''' 报文帧ID
            ''' </summary>
            Dim ID As UInteger
            ''' <summary>
            ''' 接收到信息帧时的时间标识,从CAN控制器初始化开始计时,单位微秒
            ''' </summary>
            Dim TimeStamp As UInteger
            ''' <summary>
            ''' 是否使用时间标识,为1时TimeStamp有效
            ''' </summary>
            Dim TimeFlag As Byte
            ''' <summary>
            ''' 发送帧类型 0为正常发送 1为单次发送 2为自发自收 3为单次自发自收
            ''' </summary>
            Dim SendType As Byte
            ''' <summary>
            ''' 是否是远程帧 0时为数据帧,1时为远程帧
            ''' </summary>
            Dim RemoteFlag As Byte
            ''' <summary>
            ''' 是否是扩展帧 0时为标准帧(11位帧ID),=1时为扩展帧(29位帧ID)
            ''' </summary>
            Dim ExternFlag As Byte


            ''' <summary>
            ''' 报文数据长度
            ''' </summary>
            Dim DataLen As Byte
            ''' <summary>
            ''' 报文数据
            ''' </summary>
            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)>
            Dim data As Byte()

            ''' <summary>
            ''' 系统保留
            ''' </summary>
            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)>
            Dim Reserved As Byte()
        End Structure

        ''' <summary>
        ''' 初始化CAN配置
        ''' </summary>
        Public Structure INIT_CONFIG
            ''' <summary>
            ''' 验收码
            ''' </summary>
            Dim AccCode As UInteger
            ''' <summary>
            ''' 屏蔽码 推荐为0xFFFF全部接收
            ''' </summary>
            Dim AccMask As UInteger
            ''' <summary>
            ''' 保留
            ''' </summary>
            Dim reserved As UInteger
            ''' <summary>
            ''' 滤波使能 0不使能,1使能
            ''' </summary>
            Dim Filter As Byte

            '波特率 定时器0 定时器1
            '5Kbps 0xBF 0xFF
            '10Kbps 0x31 0x1C
            '20Kbps 0x18 0x1C
            '40Kbps 0x87 0xFF
            '50Kbps 0x09 0x1C
            '80Kbps 0x83 0XFF
            '100Kbps 0x04 0x1C
            '125Kbps 0x03 0x1C
            '200Kbps 0x81 0xFA
            '250Kbps 0x01 0x1C
            '400Kbps 0x80 0xFA
            '500Kbps 0x00 0x1C
            '666Kbps 0x80 0xB6
            '800Kbps 0x00 0x16
            '1000Kbps 0x00 0x14
            ''' <summary>
            ''' 波特率0
            ''' </summary>
            Dim Timing0 As Byte
            ''' <summary>
            ''' 波特率1
            ''' </summary>
            Dim Timing1 As Byte

            ''' <summary>
            ''' 模式 0为正常模式,1为只听模式,2为自发自收模式
            ''' </summary>
            Dim Mode As Byte
        End Structure

        ''' <summary>
        ''' 打开设备
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="Reserved">无意义</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="OpenDevice")>
        Public Function OpenDevice(ByVal DeviceType As UInt32,
                                   ByVal DeviceInd As UInt32,
                                   ByVal Reserved As UInt32) As ECANStatus
        End Function

        ''' <summary>
        ''' 关闭设备
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="CloseDevice")>
        Public Function CloseDevice(ByVal DeviceType As UInt32,
                                    ByVal DeviceInd As UInt32) As ECANStatus
        End Function

        ''' <summary>
        ''' 初始化指定的CAN通道
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="CANInd"></param>
        ''' <param name="InitConfig"></param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="InitCAN")>
        Public Function InitCAN(ByVal DeviceType As UInt32,
                                ByVal DeviceInd As UInt32,
                                ByVal CANInd As UInt32,
                                ByRef InitConfig As INIT_CONFIG) As ECANStatus
        End Function

        ''' <summary>
        ''' 启动USBCAN设备的某一个CAN通道
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="CANInd">第几路CAN</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="StartCAN")>
        Public Function StartCAN(ByVal DeviceType As UInt32,
                                 ByVal DeviceInd As UInt32,
                                 ByVal CANInd As UInt32) As ECANStatus
        End Function

        ''' <summary>
        ''' 复位CAN
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="CANInd">第几路CAN</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="ResetCAN")>
        Public Function ResetCAN(ByVal DeviceType As UInt32,
                                 ByVal DeviceInd As UInt32,
                                 ByVal CANInd As UInt32) As ECANStatus
        End Function

        ''' <summary>
        ''' 发送数据
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="CANInd">第几路CAN</param>
        ''' <param name="Send">数据帧</param>
        ''' <param name="length">数据帧长度</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="Transmit")>
        Public Function Transmit(ByVal DeviceType As UInt32,
                                 ByVal DeviceInd As UInt32,
                                 ByVal CANInd As UInt32,
                                 ByRef Send As CAN_OBJ,
                                 ByVal length As UInt16) As ECANStatus
        End Function

        ''' <summary>
        ''' 读取数据
        ''' </summary>
        ''' <param name="DeviceType">设备类型号 USBCAN I 选择3,USBCAN II 选择4</param>
        ''' <param name="DeviceInd">设备索引号</param>
        ''' <param name="CANInd">第几路CAN</param>
        ''' <param name="mReceive">接收的数据帧</param>
        ''' <param name="length">数据帧长度</param>
        ''' <param name="WaitTime">超时时间 ms</param>
        ''' <returns></returns>
        <DllImport(".\DLL\ECanVci.dll", EntryPoint:="Receive")>
        Public Function Receive(ByVal DeviceType As UInt32,
                                ByVal DeviceInd As UInt32,
                                ByVal CANInd As UInt32,
                                ByRef mReceive As CAN_OBJ,
                                ByVal length As UInt32,
                                ByVal WaitTime As UInt32) As ECANStatus
        End Function

    End Module
End Namespace


