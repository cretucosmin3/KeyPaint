using System;
using System.Collections.Generic;
using Silk.NET.Input;

namespace KeyPaint;

public class DrawEngine
{
    public readonly KeyMapper KeyMapper = new();

    public DrawEngine()
    {
        AssignHotkeys();
    }

    private void AssignHotkeys()
    {
        KeyMapper.AddKeyUpRestriction(new Key[] { Key.Left, Key.Right, Key.Down, Key.Up });

        KeyMapper.OnKeyDown(Key.Left).Perform(() => { });
        KeyMapper.OnKeyDown(Key.Right).Perform(() => { });
        KeyMapper.OnKeyDown(Key.Down).Perform(() => { });
        KeyMapper.OnKeyDown(Key.Up).Perform(() => { });

        KeyMapper.OnKeyUp(Key.Left).Perform(() => { });
        KeyMapper.OnKeyUp(Key.Right).Perform(() => { });
        KeyMapper.OnKeyUp(Key.Down).Perform(() => { });
        KeyMapper.OnKeyUp(Key.Up).Perform(() => { });

        KeyMapper.OnKeyDown(Key.C).Perform(() => { });
        KeyMapper.OnKeyUp(Key.C).Perform(() => { });

        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.S }).Perform(SaveImageToFile);

        // Line width controls
        KeyMapper.OnHotkeyDown(new Key[] { Key.S, Key.Left }).Perform(() => ChangeLineWidth(-1));
        KeyMapper.OnHotkeyDown(new Key[] { Key.S, Key.Right }).Perform(() => ChangeLineWidth(1));

        // Line roundness
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Left }).Perform(() => ChangeLineRoundness(-1));
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Right }).Perform(() => ChangeLineRoundness(1));

        // Line fuzyness
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Down }).Perform(() => ChangeLineFuzyness(-1));
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Up }).Perform(() => ChangeLineFuzyness(1));

        // Shift selection
        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Left }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Right }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Down }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Up }).Perform(() => { });

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Left }).Perform(() => { });
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Right }).Perform(() => { });
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Down }).Perform(() => { });
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Up }).Perform(() => { });

        // Shift selection by half
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Left }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Right }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Down }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Up }).Perform(() => { });
    }

    private void SaveImageToFile()
    {

    }

    private void ChangeLineWidth(int increase)
    {

    }

    private void ChangeLineRoundness(int increase)
    {

    }

    private void ChangeLineFuzyness(int increase)
    {

    }
}