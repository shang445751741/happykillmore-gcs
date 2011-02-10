'*****************************************************************************
' * C# Joystick Library - Copyright (c) 2006 Mark Harris - MarkH@rris.com.au
' ******************************************************************************
' * You may use this library in your application, however please do give credit
' * to me for writing it and supplying it. If you modify this library you must
' * leave this notice at the top of this file. I'd love to see any changes you
' * do make, so please email them to me :)
' ****************************************************************************

Imports System.Collections.Generic
Imports System.Text
Imports System.Diagnostics
Imports Microsoft.DirectX
Imports Microsoft.DirectX.DirectInput

Namespace JoystickInterface
    ''' <summary>
    ''' Class to interface with a joystick device.
    ''' </summary>
    Public Class Joystick
        Private joystickDevice As Device
        Public state As JoystickState
        Private m_axis(0 To 7) As Integer
        Private m_buttons() As Boolean

        Public ReadOnly Property Axis() As Integer()
            Get
                Return m_axis
            End Get
        End Property
        '''' Array of buttons availiable on the joystick. This also includes PoV hats.
        '''' </summary>
        Public ReadOnly Property Buttons() As Boolean()
            Get
                Return m_buttons
            End Get
        End Property

        Private systemJoysticks As String()
        Private hWnd As IntPtr

        ''' <summary>
        ''' Constructor for the class.
        ''' </summary>
        ''' <param name="window_handle">Handle of the window which the joystick will be "attached" to.</param>
        Public Sub New(ByVal window_handle As IntPtr)
            hWnd = window_handle
            m_axis(0) = -1
            m_axis(1) = -1
            m_axis(2) = -1
            m_axis(3) = -1
            m_axis(4) = -1
            m_axis(5) = -1
            m_axis(6) = -1
            m_axis(7) = -1
        End Sub

        Private Sub Poll()
            Try
                ' poll the joystick
                joystickDevice.Poll()
                ' update the joystick state field
                state = joystickDevice.CurrentJoystickState
            Catch err As Exception
                ' we probably lost connection to the joystick
                ' was it unplugged or locked by another application?
                Debug.WriteLine("Poll()")
                Debug.WriteLine(err.Message)
                Debug.WriteLine(err.StackTrace)
            End Try
        End Sub

        ''' <summary>
        ''' Retrieves a list of joysticks attached to the computer.
        ''' </summary>
        ''' <example>
        ''' [C#]
        ''' <code>
        ''' JoystickInterface.Joystick jst = new JoystickInterface.Joystick(this.Handle);
        ''' string[] sticks = jst.FindJoysticks();
        ''' </code>
        ''' </example>
        ''' <returns>A list of joysticks as an array of strings.</returns>
        Public Function FindJoysticks() As String()
            systemJoysticks = Nothing

            Try
                ' Find all the GameControl devices that are attached.
                Dim gameControllerList As DeviceList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly)

                ' check that we have at least one device.
                If gameControllerList.Count > 0 Then
                    systemJoysticks = New String(gameControllerList.Count - 1) {}
                    Dim i As Integer = 0
                    ' loop through the devices.
                    For Each deviceInstance As DeviceInstance In gameControllerList
                        ' create a device from this controller so we can retrieve info.
                        joystickDevice = New Device(deviceInstance.InstanceGuid)
                        joystickDevice.SetCooperativeLevel(hWnd, CooperativeLevelFlags.Background Or CooperativeLevelFlags.NonExclusive)

                        systemJoysticks(i) = joystickDevice.DeviceInformation.InstanceName

                        i += 1
                    Next
                End If
            Catch err As Exception
                Debug.WriteLine("FindJoysticks()")
                Debug.WriteLine(err.Message)
                Debug.WriteLine(err.StackTrace)
            End Try

            Return systemJoysticks
        End Function

        ''' <summary>
        ''' Acquire the named joystick. You can find this joystick through the <see cref="FindJoysticks"/> method.
        ''' </summary>
        ''' <param name="name">Name of the joystick.</param>
        ''' <returns>The success of the connection.</returns>
        Public Function AcquireJoystick(ByVal name As String) As Boolean
            Try
                Dim gameControllerList As DeviceList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly)
                Dim i As Integer = 0
                Dim found As Boolean = False
                ' loop through the devices.
                For Each deviceInstance As DeviceInstance In gameControllerList
                    If deviceInstance.InstanceName = name Then
                        found = True
                        ' create a device from this controller so we can retrieve info.
                        joystickDevice = New Device(deviceInstance.InstanceGuid)
                        joystickDevice.SetCooperativeLevel(hWnd, CooperativeLevelFlags.Background Or CooperativeLevelFlags.NonExclusive)
                        Exit For
                    End If

                    i += 1
                Next

                If Not found Then
                    Return False
                End If

                ' Tell DirectX that this is a Joystick.
                joystickDevice.SetDataFormat(DeviceDataFormat.Joystick)

                ' Finally, acquire the device.
                joystickDevice.Acquire()

                ' How many axes?
                ' Find the capabilities of the joystick
                Dim cps As DeviceCaps = joystickDevice.Caps
                'Debug.Print("Joystick Axis: " & cps.NumberAxes)
                'Debug.Print("Joystick Buttons: " & cps.NumberButtons)

                UpdateStatus()
            Catch err As Exception
                Debug.WriteLine("FindJoysticks()")
                Debug.WriteLine(err.Message)
                Debug.WriteLine(err.StackTrace)
                Return False
            End Try

            Return True
        End Function

        ''' <summary>
        ''' Unaquire a joystick releasing it back to the system.
        ''' </summary>
        Public Sub ReleaseJoystick()
            joystickDevice.Unacquire()
        End Sub

        ''' <summary>
        ''' Update the properties of button and axis positions.
        ''' </summary>
        Public Sub UpdateStatus()
            Poll()

            'Rz Rx X Y Axis1 Axis2
            m_axis(0) = state.Ry
            m_axis(1) = state.Rx
            m_axis(2) = state.Rz
            m_axis(3) = state.Y
            m_axis(4) = state.X
            m_axis(5) = state.Z
            Try
                m_axis(6) = state.GetSlider(0)
                m_axis(7) = state.GetSlider(1)
            Catch
            End Try

            ' not using buttons, so don't take the tiny amount of time it takes to get/parse
            Dim jsButtons As Byte() = state.GetButtons()
            If Not jsButtons Is Nothing Then
                m_buttons = New Boolean(jsButtons.Length - 1) {}

                Dim i As Integer = 0
                For Each button As Byte In jsButtons
                    m_buttons(i) = button >= 128
                    i += 1
                Next
            Else
                m_buttons = Nothing
            End If
        End Sub
    End Class
End Namespace