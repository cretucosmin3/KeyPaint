using SkiaSharp;

namespace KeyPaint;

public class DrawStyle
{
    public SKPathEffect FuzzyEffect = null!;
    public SKPathEffect RoundEffect = null!;
    public float Roundness = 0;
    public float Fuzzyness = 0;
    public bool IsFill = false;
    public bool IsClosedPath = false;
}