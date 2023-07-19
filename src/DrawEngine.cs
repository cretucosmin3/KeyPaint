using System;
using System.Collections.Generic;
using System.IO;
using NativeFileDialogSharp;
using Silk.NET.Input;
using SkiaSharp;

namespace KeyPaint;

public class DrawEngine
{
    public readonly KeyMapper KeyMapper = new();
    public Commands KeyCommands = new();
    public DrawStyle DrawStyle = new();
    public Action OnRenderRequest = default!;

    // Drawing
    SKRect FocusArea = default!;
    SKRect PreviousFocusArea = default!;
    SKRect SelectedFocusArea = default!;
    SKPoint SelectedFocusPoint
    {
        get => new SKPoint(SelectedFocusArea.MidX, SelectedFocusArea.MidY);
    }

    SKRect DrawingArea = new(0, 0, 2, 2);

    // UI
    readonly SKRoundRect UIPanelArea = new(new(6, 6, 66, 66), 4);
    readonly SKPath UILineExample = new();
    bool DisplayPreview = true;

    readonly List<SKPoint> DrawPathPoints = new();
    readonly SKPath CurrentPath = new();

    bool IsPlacingPoint
    {
        get => SelectedFocusArea != DrawingArea;
    }

    bool HasAreaFocused
    {
        get => FocusArea != DrawingArea;
    }

    bool HasAreaSelected
    {
        get => SelectedFocusArea != DrawingArea;
    }

    private SKBitmap DrawingBitmap = default!;
    SKCanvas BitmapCanvas = default!;

    public DrawEngine()
    {
        AssignKeys();
        RedoPreviewPath();
    }

    public void SetCanvasSize(int width, int height)
    {
        DrawingArea = new SKRect(0, 0, width, height);
        ResetDrawing();
    }

    private void AssignKeys()
    {
        KeyMapper.AddKeyUpRestriction(new Key[] { Key.Left, Key.Right, Key.Down, Key.Up });

        #region Style Hotkeys
        // Line width controls
        KeyMapper.OnHotkeyDown(new Key[] { Key.S, Key.Left }).Perform(() => ChangeLineWidth(-1));
        KeyMapper.OnHotkeyDown(new Key[] { Key.S, Key.Right }).Perform(() => ChangeLineWidth(1));

        // Line roundness
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Left }).Perform(() => ChangeRoundness(-4));
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Right }).Perform(() => ChangeRoundness(4));

        // Line fuzyness
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Down }).Perform(() => ChangeFuzzyness(-1));
        KeyMapper.OnHotkeyDown(new Key[] { Key.D, Key.Up }).Perform(() => ChangeFuzzyness(1));

        // Fill
        KeyMapper.OnKeyUp(Key.F).Perform(ToggleFill);

        // Closed path
        KeyMapper.OnKeyUp(Key.G).Perform(ToggleClosedPath);

        // Select colors
        KeyMapper.OnKeyUp(Key.Number1).Perform(() => PaintsLibrary.DrawPaint.Color = new(235, 235, 235));
        KeyMapper.OnKeyUp(Key.Number2).Perform(() => PaintsLibrary.DrawPaint.Color = new(175, 175, 175));
        KeyMapper.OnKeyUp(Key.Number3).Perform(() => PaintsLibrary.DrawPaint.Color = new(110, 110, 110));
        KeyMapper.OnKeyUp(Key.Number4).Perform(() => PaintsLibrary.DrawPaint.Color = new(50, 50, 50));
        KeyMapper.OnKeyUp(Key.Number5).Perform(() => PaintsLibrary.DrawPaint.Color = new(15, 15, 15));
        #endregion

        #region Drawing Hotkeys
        // Move focus area
        KeyMapper.OnKeyDown(Key.Left).Perform(() => OnDirectionPressed(Direction.Left));
        KeyMapper.OnKeyDown(Key.Right).Perform(() => OnDirectionPressed(Direction.Right));
        KeyMapper.OnKeyDown(Key.Down).Perform(() => OnDirectionPressed(Direction.Down));
        KeyMapper.OnKeyDown(Key.Up).Perform(() => OnDirectionPressed(Direction.Up));

        // Confirm focus area
        KeyMapper.OnKeyUp(Key.Left).Perform(CalculateSelection);
        KeyMapper.OnKeyUp(Key.Right).Perform(CalculateSelection);
        KeyMapper.OnKeyUp(Key.Down).Perform(CalculateSelection);
        KeyMapper.OnKeyUp(Key.Up).Perform(CalculateSelection);

        KeyMapper.OnKeyUp(Key.C).Perform(OnPointRelease);
        KeyMapper.OnKeyUp(Key.V).Perform(GoToLastFocusArea);
        KeyMapper.OnKeyUp(Key.Space).Perform(ConfirmCurrentPath);
        KeyMapper.OnKeyUp(Key.Z).Perform(Undo);
        KeyMapper.OnKeyUp(Key.X).Perform(ClearCurrentPath);
        KeyMapper.OnKeyUp(Key.Q).Perform(() => BitmapCanvas.Clear(SKColors.White));

        // Shift selection
        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Left }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, -1f, 0)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Right }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, 1f, 0)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Down }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, 1f)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.V, Key.Up }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, -1f)
        );

        // Shift confirmation
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Left }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Right }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Down }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.V, Key.Up }).Perform(ConfirmShiftArea);

        // Shift selection by half
        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Left }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, -1f, 0)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Right }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, DrawingArea, 1f, 0)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Down }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, -1f)
        );

        KeyMapper.OnHotkeyDown(new Key[] { Key.ControlLeft, Key.V, Key.Up }).Perform(
            () => SelectedFocusArea = AreaHelper.ShiftArea(SelectedFocusArea, FocusArea, 0, 1f)
        );

        // Shift by half confirmation
        KeyMapper.OnHotkeyUp(new Key[] { Key.ControlLeft, Key.V, Key.Left }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.ControlLeft, Key.V, Key.Right }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.ControlLeft, Key.V, Key.Down }).Perform(ConfirmShiftArea);
        KeyMapper.OnHotkeyUp(new Key[] { Key.ControlLeft, Key.V, Key.Up }).Perform(ConfirmShiftArea);
        #endregion

        #region Misc Binds
        KeyMapper.OnKeyUp(Key.Tab).Perform(() => DisplayPreview = !DisplayPreview);
        KeyMapper.OnHotkeyDown(KeyCommands.Export_Image).Perform(SaveImageToFile);
        #endregion

        KeyMapper.OnAfterAnyEvent += () =>
        {
            RedoCurrentPath();
            OnRenderRequest?.Invoke();
        };
    }

    #region Command Actions

    private void OnDirectionPressed(Direction direction)
    {
        SelectedFocusArea = AreaHelper.TrimAreaToDirection(FocusArea, direction);

        RedoCurrentPath();
    }

    private void OnPointRelease()
    {
        PlaceDrawPoint();
    }

    private void SaveImageToFile()
    {
        var dialogResult = Dialog.FileSave("png");

        if (dialogResult.IsOk)
        {
            string filePath = dialogResult.Path;
            if (!filePath.Contains(".png"))
            {
                filePath += ".png";
            }

            using var data = DrawingBitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);

            // save the data to a stream
            data.SaveTo(stream);
        }
    }

    private void ChangeLineWidth(int increase)
    {
        float newThickness = PaintsLibrary.DrawPaint.StrokeWidth + increase;
        PaintsLibrary.DrawPaint.StrokeWidth = Math.Clamp(newThickness, 0.5f, 20);
    }

    private void ChangeRoundness(float increase)
    {
        // Dispose of old round effect
        DrawStyle.RoundEffect?.Dispose();

        DrawStyle.Roundness = Math.Clamp(DrawStyle.Roundness + increase, 0, 200);
        DrawStyle.RoundEffect = SKPathEffect.CreateCorner(DrawStyle.Roundness);

        ComposePathEffects();
    }

    private void ChangeFuzzyness(float increase)
    {
        // Dispose of old fuzzy effect
        DrawStyle.FuzzyEffect?.Dispose();

        DrawStyle.Fuzzyness = Math.Clamp(DrawStyle.Fuzzyness + increase, 0, 10);
        DrawStyle.FuzzyEffect = SKPathEffect.CreateDiscrete(DrawStyle.Fuzzyness * 1.5f, DrawStyle.Fuzzyness);

        ComposePathEffects();
    }

    private void ToggleFill()
    {
        DrawStyle.IsFill = !DrawStyle.IsFill;
        PaintsLibrary.DrawPaint.Style = DrawStyle.IsFill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
    }

    private void ToggleClosedPath()
    {
        DrawStyle.IsClosedPath = !DrawStyle.IsClosedPath;
        RedoCurrentPath();

    }

    private void ConfirmCurrentPath()
    {
        if (DrawPathPoints.Count > 1)
            BitmapCanvas.DrawPath(CurrentPath, PaintsLibrary.DrawPaint);

        CurrentPath.Rewind();
        DrawPathPoints.Clear();
    }

    #endregion

    #region On Canvas Functionalities

    private void ResetDrawing()
    {
        BitmapCanvas?.Dispose();
        DrawingBitmap?.Dispose();

        // Reset areas
        FocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);
        PreviousFocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);
        SelectedFocusArea = new(DrawingArea.Left, DrawingArea.Top, DrawingArea.Right, DrawingArea.Bottom);

        DrawingBitmap = new((int)DrawingArea.Width, (int)DrawingArea.Height, true);
        BitmapCanvas = new(DrawingBitmap);
        BitmapCanvas.Clear(SKColors.White);
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
            CurrentPath.LineTo(SelectedFocusPoint);
        }

        if (DrawStyle.IsClosedPath)
            CurrentPath.Close();
    }

    private void ComposePathEffects()
    {
        bool isRound = DrawStyle.RoundEffect != null;
        bool isFuzzy = DrawStyle.FuzzyEffect != null;

        PaintsLibrary.DrawPaint.PathEffect = null;

        if (isRound && isFuzzy) // Round and fuzzy
        {
            PaintsLibrary.DrawPaint.PathEffect =
                SKPathEffect.CreateCompose(DrawStyle.FuzzyEffect, DrawStyle.RoundEffect);
        }
        else if (isRound && !isFuzzy) // Round but NOT fuzzy
        {
            PaintsLibrary.DrawPaint.PathEffect = DrawStyle.RoundEffect;
        }
        else if (isFuzzy && !isRound) // Fuzzy but NOT round
        {
            PaintsLibrary.DrawPaint.PathEffect = DrawStyle.FuzzyEffect;
        }
    }

    private void ConfirmShiftArea()
    {
        if (IsPlacingPoint)
        {
            FocusArea.Left = SelectedFocusArea.Left;
            FocusArea.Right = SelectedFocusArea.Right;
            FocusArea.Top = SelectedFocusArea.Top;
            FocusArea.Bottom = SelectedFocusArea.Bottom;
        }
    }

    private void CalculateSelection()
    {
        FocusArea.Left = SelectedFocusArea.Left;
        FocusArea.Right = SelectedFocusArea.Right;
        FocusArea.Top = SelectedFocusArea.Top;
        FocusArea.Bottom = SelectedFocusArea.Bottom;

        RedoCurrentPath();
    }

    private void PlaceDrawPoint()
    {
        DrawPathPoints.Add(new(
            FocusArea.MidX,
            FocusArea.MidY
        ));

        PreviousFocusArea.Left = FocusArea.Left;
        PreviousFocusArea.Right = FocusArea.Right;
        PreviousFocusArea.Top = FocusArea.Top;
        PreviousFocusArea.Bottom = FocusArea.Bottom;

        RedoCurrentPath();
        ResetFocusArea();
    }

    private void Undo()
    {
        // TODO: implement undo on focus area

        if (DrawPathPoints.Count == 1)
            DrawPathPoints.Clear();

        if (DrawPathPoints.Count > 0 && !IsPlacingPoint)
        {
            DrawPathPoints.RemoveAt(DrawPathPoints.Count - 1);
        }

        ResetFocusArea();
        RedoCurrentPath();
    }

    private void ClearCurrentPath()
    {
        DrawPathPoints.Clear();

        ResetFocusArea();
        RedoCurrentPath();
    }

    private void GoToLastFocusArea()
    {
        if (!IsPlacingPoint)
        {
            FocusArea.Left = PreviousFocusArea.Left;
            FocusArea.Right = PreviousFocusArea.Right;
            FocusArea.Top = PreviousFocusArea.Top;
            FocusArea.Bottom = PreviousFocusArea.Bottom;

            SelectedFocusArea.Left = FocusArea.Left;
            SelectedFocusArea.Top = FocusArea.Top;
            SelectedFocusArea.Right = FocusArea.Right;
            SelectedFocusArea.Bottom = FocusArea.Bottom;

            RedoCurrentPath();
        }
    }

    private void ResetFocusArea()
    {
        FocusArea.Left = 0;
        FocusArea.Top = 0;
        FocusArea.Right = DrawingArea.Width;
        FocusArea.Bottom = DrawingArea.Height;

        SelectedFocusArea.Left = 0;
        SelectedFocusArea.Top = 0;
        SelectedFocusArea.Right = DrawingArea.Width;
        SelectedFocusArea.Bottom = DrawingArea.Height;
    }

    #endregion

    #region Actual Drawing

    public void DrawOnCanvas(SKCanvas canvas)
    {
        canvas.DrawBitmap(DrawingBitmap, 0, 0);

        if (DrawPathPoints.Count == 1)
        {
            canvas.DrawPoint(DrawPathPoints[0], PaintsLibrary.DrawPaint);
        }

        if (!DrawStyle.IsClosedPath && IsPlacingPoint && DrawPathPoints.Count > 0)
        {
            CurrentPath.LineTo(SelectedFocusPoint);
        }

        if (DrawPathPoints.Count > 0)
        {

            canvas.DrawPath(CurrentPath, PaintsLibrary.DrawPaint);
        }

        if (HasAreaSelected)
        {
            canvas.DrawRect(FocusArea, PaintsLibrary.FocusAreaPaint);

            if (HasAreaSelected)
            {
                canvas.DrawRect(SelectedFocusArea, PaintsLibrary.SelectedFocusAreaPaint);

                PaintsLibrary.FocusPointPaint.StrokeWidth = Math.Clamp(PaintsLibrary.DrawPaint.StrokeWidth + 3, 6, 100);

                canvas.DrawPoint(SelectedFocusPoint, PaintsLibrary.FocusPointPaint);

                // Draw horizontal guide line
                canvas.DrawLine(0, SelectedFocusPoint.Y, DrawingArea.Width, SelectedFocusPoint.Y, PaintsLibrary.CrossPointPaint);

                // Draw vertical guide line
                canvas.DrawLine(SelectedFocusPoint.X, 0, SelectedFocusPoint.X, DrawingArea.Height, PaintsLibrary.CrossPointPaint);
            }
        }

        if (DisplayPreview)
            DrawUserInterface(canvas);
    }

    void DrawUserInterface(SKCanvas canvas)
    {
        bool DrawingPointTooClose = SelectedFocusPoint.X < 150 && SelectedFocusPoint.Y < 150;
        bool ShowToRight = IsPlacingPoint && DrawingPointTooClose;

        var previousColor = PaintsLibrary.DrawPaint.Color;
        PaintsLibrary.DrawPaint.Color = SKColors.Black;

        if (ShowToRight)
        {
            UIPanelArea.Offset(DrawingArea.Width - 72, 0);
            UILineExample.Offset(DrawingArea.Width - 72, 0);
        }

        canvas.DrawRoundRect(UIPanelArea, PaintsLibrary.UIPanel);

        PaintsLibrary.UIPanelOutline.Color = DrawPathPoints.Count > 0 ? SKColors.Black : SKColors.LightGray;
        PaintsLibrary.UIPanelOutline.StrokeWidth = DrawPathPoints.Count > 0 ? 3 : 2;

        canvas.DrawRoundRect(UIPanelArea, PaintsLibrary.UIPanelOutline);

        RedoPreviewPath();
        canvas.DrawPath(UILineExample, PaintsLibrary.DrawPaint);

        PaintsLibrary.DrawPaint.Color = previousColor;

        if (ShowToRight)
        {
            UIPanelArea.Offset(0 - (DrawingArea.Width - 72), 0);
            UILineExample.Offset(0 - (DrawingArea.Width - 72), 0);
        }
    }

    void RedoPreviewPath()
    {
        UILineExample.Rewind();
        UILineExample.MoveTo(18, 52);
        UILineExample.LineTo(52, 20);
        UILineExample.LineTo(52, 52);
        UILineExample.LineTo(18, 20);

        if (DrawStyle.IsClosedPath)
            UILineExample.Close();
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