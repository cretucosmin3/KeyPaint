using SkiaSharp;

namespace KeyPaint
{
    static class PaintsLibrary
    {

        #region Drawing
        public static readonly SKPaint DrawPaint = new()
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        public static readonly SKPaint FocusAreaPaint = new()
        {
            Color = SKColors.Red.WithAlpha(175),
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
        };

        public static readonly SKPaint FocusPointPaint = new()
        {
            Color = SKColors.IndianRed,
            StrokeWidth = 6,
            Style = SKPaintStyle.Stroke,
        };

        public static readonly SKPaint CrossPointPaint = new()
        {
            Color = SKColors.Black.WithAlpha(60),
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 4, 8 }, 0)
        };

        public static readonly SKPaint SelectedFocusAreaPaint = new()
        {
            Color = SKColors.Red.WithAlpha(25),
            Style = SKPaintStyle.Fill,
        };
        #endregion

        #region User Interface
        public static readonly SKPaint UIPanel = new()
        {
            Color = new(225, 225, 225),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2
        };
        public static readonly SKPaint UIPanelOutline = new()
        {
            Color = new(100, 100, 100),
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1.5f
        };
        #endregion
    }
}