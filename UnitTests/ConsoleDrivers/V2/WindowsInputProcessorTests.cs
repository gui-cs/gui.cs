﻿using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers;
using InputRecord = Terminal.Gui.WindowsConsole.InputRecord;

namespace UnitTests.ConsoleDrivers.V2;
public class WindowsInputProcessorTests
{

    [Fact]
    public void Test_ProcessQueue_CapitalHLowerE ()
    {
        var queue = new ConcurrentQueue<InputRecord> ();

        queue.Enqueue (new  InputRecord()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = true,
                UnicodeChar = 'H',
                dwControlKeyState = WindowsConsole.ControlKeyState.CapslockOn,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)72,
                wVirtualScanCode = 35
            }
        });
        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = false,
                UnicodeChar = 'H',
                dwControlKeyState = WindowsConsole.ControlKeyState.CapslockOn,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)72,
                wVirtualScanCode = 35
            }
        });
        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = true,
                UnicodeChar = 'i',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)73,
                wVirtualScanCode = 23
            }
        });
        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = false,
                UnicodeChar = 'i',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)73,
                wVirtualScanCode = 23
            }
        });

        var processor = new WindowsInputProcessor (queue);

        List<Key> ups = new List<Key> ();
        List<Key> downs = new List<Key> ();

        processor.KeyUp += (s, e) => { ups.Add (e); };
        processor.KeyDown += (s, e) => { downs.Add (e); };

        Assert.Empty (ups);
        Assert.Empty (downs);

        processor.ProcessQueue ();

        Assert.Equal (Key.H.WithShift, ups [0]);
        Assert.Equal (Key.H.WithShift, downs [0]);
        Assert.Equal (Key.I, ups [1]);
        Assert.Equal (Key.I, downs [1]);
    }


    [Fact]
    public void Test_ProcessQueue_Mouse_Move ()
    {
        var queue = new ConcurrentQueue<InputRecord> ();

        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = new WindowsConsole.MouseEventRecord
            {
                MousePosition = new WindowsConsole.Coord(32,31),
                ButtonState = WindowsConsole.ButtonState.NoButtonPressed,
                ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                EventFlags = WindowsConsole.EventFlags.MouseMoved
            }
        });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new List<MouseEventArgs> ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        var s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags,MouseFlags.ReportMousePosition);
        Assert.Equal (s.ScreenPosition,new Point (32,31));
    }

    [Theory]
    [InlineData(WindowsConsole.ButtonState.Button1Pressed,MouseFlags.Button1Pressed)]
    [InlineData (WindowsConsole.ButtonState.Button2Pressed, MouseFlags.Button2Pressed)]
    [InlineData (WindowsConsole.ButtonState.Button3Pressed, MouseFlags.Button3Pressed)]
    [InlineData (WindowsConsole.ButtonState.Button4Pressed, MouseFlags.Button4Pressed)]
    internal void Test_ProcessQueue_Mouse_Pressed (WindowsConsole.ButtonState state,MouseFlags expectedFlag )
    {
        var queue = new ConcurrentQueue<InputRecord> ();

        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = new WindowsConsole.MouseEventRecord
            {
                MousePosition = new WindowsConsole.Coord (32, 31),
                ButtonState = state,
                ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                EventFlags = WindowsConsole.EventFlags.MouseMoved
            }
        });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new List<MouseEventArgs> ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        var s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags, MouseFlags.ReportMousePosition | expectedFlag);
        Assert.Equal (s.ScreenPosition, new Point (32, 31));
    }


    [Theory]
    [InlineData (100, MouseFlags.WheeledUp)]
    [InlineData ( -100, MouseFlags.WheeledDown)]
    internal void Test_ProcessQueue_Mouse_Wheel (int wheelValue, MouseFlags expectedFlag)
    {
        var queue = new ConcurrentQueue<InputRecord> ();

        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = new WindowsConsole.MouseEventRecord
            {
                MousePosition = new WindowsConsole.Coord (32, 31),
                ButtonState = (WindowsConsole.ButtonState)wheelValue,
                ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                EventFlags = WindowsConsole.EventFlags.MouseWheeled
            }
        });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new List<MouseEventArgs> ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        var s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags,expectedFlag);
        Assert.Equal (s.ScreenPosition, new Point (32, 31));
    }
}

