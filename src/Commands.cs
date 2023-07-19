using Silk.NET.Input;

namespace KeyPaint;

public class Commands
{
    #region Canvas Commands
    /// <summary> Ctrl + S </summary>
    public Key[] Export_Image = new Key[] { Key.ControlLeft, Key.S };
    #endregion

    #region Style Commands

    public Key[] Line_Width_Increase = new Key[] { Key.S, Key.Right };
    public Key[] Line_Width_Decrease = new Key[] { Key.S, Key.Left };

    // Line Roundness
    public Key[] Line_Roundness_Increase = new Key[] { Key.D, Key.Right };
    public Key[] Line_Roundness_Decrease = new Key[] { Key.D, Key.Left };

    // Line Fuzyness
    public Key[] Line_Fuzyness_Increase = new Key[] { Key.D, Key.Up };
    public Key[] Line_Fuzyness_Decrease = new Key[] { Key.D, Key.Down };

    #endregion

    #region Drawing Commands
    public Key Clear_All = Key.Q;
    public Key Undo_Point = Key.Z;
    public Key Confirm_Draw_Point = Key.C;
    public Key Confirm_Current_Path = Key.Space;

    // Moving point
    public Key Move_Point_Left = Key.Left;
    public Key Move_Point_Right = Key.Right;
    public Key Move_Point_Down = Key.Down;
    public Key Move_Point_Up = Key.Up;

    // Shift point
    public Key[] Shift_Point_Left = new Key[] { Key.V, Key.Left };
    public Key[] Shift_Point_Right = new Key[] { Key.V, Key.Right };
    public Key[] Shift_Point_Down = new Key[] { Key.V, Key.Down };
    public Key[] Shift_Point_Up = new Key[] { Key.V, Key.Up };

    // Shift point by half unit
    public Key[] Shift_Point_Left_Half = new Key[] { Key.ControlLeft, Key.V, Key.Left };
    public Key[] Shift_Point_Right_Half = new Key[] { Key.ControlLeft, Key.V, Key.Right };
    public Key[] Shift_Point_Down_Half = new Key[] { Key.ControlLeft, Key.V, Key.Down };
    public Key[] Shift_Point_Up_Half = new Key[] { Key.ControlLeft, Key.V, Key.Up };


    #endregion
}