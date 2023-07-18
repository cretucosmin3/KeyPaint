using System;
using System.Collections.Generic;
using Silk.NET.Input;
using SkiaSharp;

namespace KeyPaint;

public class DrawEngine
{

    public readonly KeyMapper KeyMapper = new();
    public Commands KeyCommands = new();

    const float WindowWidth = 1200;
    const float WindowHeight = 850;

    // Drawing
    SKRect FocusArea = new(0, 0, WindowWidth, WindowHeight);
    SKRect PreviousFocusArea = new(0, 0, WindowWidth, WindowHeight);
    SKRect SelectedFocusArea = new(0, 0, WindowWidth, WindowHeight);
    SKPoint SelectedFocusPoint = new(0, 0);

    // UI
    readonly SKRoundRect UIPanelArea = new(new(6, 6, 66, 66), 4);
    readonly SKPath UILineExample = new();
    bool DisplayPreview = true;

    readonly List<SKPoint> DrawPathPoints = new();
    readonly SKPath CurrentPath = new();

    SKPathEffect FuzzyEffect = null!;
    SKPathEffect RoundEffect = null!;
    float Roundness = 0;
    float Fuzzyness = 0;

    bool IsPlacingPoint = false;
    bool HasAreaFocused = false;
    bool SelectingRoundnessAndFuzzyness = false;
    bool IsSelectingThinkness = false;
    Silk.NET.Input.Key FocusedDirection;

    readonly SKBitmap DrawingBitmap = new((int)WindowWidth, (int)WindowHeight, true);
    SKCanvas BitmapCanvas = null!;

    public DrawEngine()
    {
        AssignKeys();
    }

    private void AssignKeys()
    {
        KeyMapper.AddKeyUpRestriction(new Key[] { Key.Left, Key.Right, Key.Down, Key.Up });

        // Direction keys down
        KeyMapper.OnKeyDown(Key.Left).Perform(() => OnDirectionPressed(Direction.Left));
        KeyMapper.OnKeyDown(Key.Right).Perform(() => OnDirectionPressed(Direction.Right));
        KeyMapper.OnKeyDown(Key.Down).Perform(() => OnDirectionPressed(Direction.Down));
        KeyMapper.OnKeyDown(Key.Up).Perform(() => OnDirectionPressed(Direction.Up));

        // Direction keys up
        KeyMapper.OnKeyUp(Key.Left).Perform(() => OnDirectionReleased(Direction.Left));
        KeyMapper.OnKeyUp(Key.Right).Perform(() => OnDirectionReleased(Direction.Right));
        KeyMapper.OnKeyUp(Key.Down).Perform(() => OnDirectionReleased(Direction.Down));
        KeyMapper.OnKeyUp(Key.Up).Perform(() => OnDirectionReleased(Direction.Up));

        KeyMapper.OnKeyDown(Key.C).Perform(() => { });
        KeyMapper.OnKeyUp(Key.Space).Perform(() => { });
        KeyMapper.OnKeyUp(Key.Tab).Perform(() => DisplayPreview = !DisplayPreview);

        KeyMapper.OnHotkeyDown(KeyCommands.Export_Image).Perform(SaveImageToFile);

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

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Left }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftFocusArea(SelectedFocusArea, FocusArea, -1f, 0)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Right }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftFocusArea(SelectedFocusArea, FocusArea, 1f, 0)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Down }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftFocusArea(SelectedFocusArea, FocusArea, 0, -1f)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Up }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftFocusArea(SelectedFocusArea, FocusArea, 0, 1f)
        );

        // Shift selection by half
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Left }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Right }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Down }).Perform(() => { });
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Up }).Perform(() => { });
    }

    #region Command Actions

    private void OnDirectionPressed(Direction direction)
    {
        IsPlacingPoint = true;
        HasAreaFocused = true;

        float Width = FocusArea.Right - FocusArea.Left;
        float Height = FocusArea.Bottom - FocusArea.Top;

        switch (FocusedDirection)
        {
            case Silk.NET.Input.Key.Left:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Silk.NET.Input.Key.Right:
                SelectedFocusArea.Left = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Silk.NET.Input.Key.Up:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Top + (Height / 2f);
                break;
            case Silk.NET.Input.Key.Down:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top + (Height / 2f);
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
        }

        RedoCurrentPath();
    }

    private void OnDirectionReleased(Direction direction)
    {

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

    #endregion

    #region On Canvas Functionalities

    private void CalculateFocusedArea(Direction direction)
    {
        float Width = FocusArea.Right - FocusArea.Left;
        float Height = FocusArea.Bottom - FocusArea.Top;

        switch (direction)
        {
            case Direction.Left:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Direction.Right:
                SelectedFocusArea.Left = FocusArea.Left + (Width / 2f);
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
            case Direction.Up:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top;
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Top + (Height / 2f);
                break;
            case Direction.Down:
                SelectedFocusArea.Left = FocusArea.Left;
                SelectedFocusArea.Top = FocusArea.Top + (Height / 2f);
                SelectedFocusArea.Right = FocusArea.Right;
                SelectedFocusArea.Bottom = FocusArea.Bottom;
                break;
        }
    }

    private void RedoCurrentPath()
    {
        CurrentPath.Rewind();

        if (DrawPathPoints.Count > 0)
        {
            // Start point
            CurrentPath.MoveTo(DrawPathPoints[0]);

            // Add lines
            for (int i = 1; i < DrawPathPoints.Count; i++)
            {
                CurrentPath.LineTo(DrawPathPoints[i]);
            }
        }

        if (DrawPathPoints.Count > 0 && IsPlacingPoint && HasAreaFocused)
        {
            SelectedFocusPoint.X = SelectedFocusArea.MidX;
            SelectedFocusPoint.Y = SelectedFocusArea.MidY;

            CurrentPath.LineTo(SelectedFocusPoint);
        }
    }

    #endregion

    public enum Direction
    {
        Left,
        Right,
        Down,
        Up
    }
}