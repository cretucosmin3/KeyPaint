using System;
using System.Collections.Generic;
using Silk.NET.Input;
using SkiaSharp;

namespace KeyPaint;

public class DrawEngine
{

    public readonly KeyMapper KeyMapper = new();
    public Commands KeyCommands = new();

    // Drawing
    SKRect FocusArea = default!;
    SKRect PreviousFocusArea = default!;
    SKRect SelectedFocusArea = default!;
    SKPoint SelectedFocusPoint = new(0, 0);
    SKRect DrawingArea = new(0, 0, 2, 2);

    // UI
    readonly SKRoundRect UIPanelArea = new(new(6, 6, 66, 66), 4);
    readonly SKPath UILineExample = new();
    bool DisplayPreview = true;

    readonly List<SKPoint> DrawPathPoints = new();
    readonly SKPath CurrentPath = new();

    bool IsPlacingPoint = false;
    bool HasAreaFocused = false;

    private SKBitmap DrawingBitmap = default!;
    SKCanvas BitmapCanvas = default!;

    public DrawEngine()
    {
        AssignKeys();
    }

    public void SetCanvasSize(int width, int height)
    {
        DrawingArea = new SKRect(0, 0, width, height);
        ResetDrawing();
    }

    private void ResetDrawing()
    {
        BitmapCanvas?.Dispose();
        DrawingBitmap?.Dispose();

        // Reset areas
        FocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);
        PreviousFocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);
        SelectedFocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);

        // Reset point position
        SelectedFocusPoint.X = -1;
        SelectedFocusPoint.Y = -1;

        DrawingBitmap = new((int)DrawingArea.Width, (int)DrawingArea.Height, true);
        BitmapCanvas = new(DrawingBitmap);
        BitmapCanvas.Clear(SKColors.White);
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
        KeyMapper.OnKeyUp(Key.Space).Perform(ConfirmCurrentPath);
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
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, -1f, 0)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Right }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, 1f, 0)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Down }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, -1f)
        );

        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Up }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, 1f)
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

        SelectedFocusArea = AreaHelper.TrimAreaToDirection(FocusArea, direction);
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

    private void ConfirmCurrentPath()
    {
        if (IsPlacingPoint) return;

        if (DrawPathPoints.Count > 1)
        {
            BitmapCanvas.DrawPath(CurrentPath, PaintsLibrary.DrawPaint);
        }

        CurrentPath.Rewind();
        DrawPathPoints.Clear();
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