namespace KeyPaint;

public static class Commands
{
    #region Canvas Commands
    public const string Save_Image_To_File = "Save_Image_To_File";
    #endregion

    #region Style Commands

    // Line Width
    public const string Line_Width_Increase = "Line_Width_Increase";
    public const string Line_Width_Decrease = "Line_Width_Decrease";

    // Line Roundness
    public const string Line_Roundness_Increase = "Line_Roundness_Increase";
    public const string Line_Roundness_Decrease = "Line_Roundness_Decrease";

    // Line Fuzyness
    public const string Line_Fuzyness_Increase = "Line_Fuzyness_Increase";
    public const string Line_Fuzyness_Decrease = "Line_Fuzyness_Decrease";

    #endregion

    #region Drawing Commands
    public const string Clear_All = "Clear_All";
    public const string Undo_Point = "Undo_Point";
    public const string Confirm_Draw_Point = "Confirm_Draw_Point";
    public const string Confirm_Current_Path = "Confirm_Current_Path";

    // Moving point
    public const string Move_Point_Left = "Move_Point_Left";
    public const string Move_Point_Right = "Move_Point_Right";
    public const string Move_Point_Down = "Move_Point_Down";
    public const string Move_Point_Up = "Move_Point_Up";

    // Shift point
    public const string Shift_Point_Left = "Shift_Point_Left";
    public const string Shift_Point_Right = "Shift_Point_Right";
    public const string Shift_Point_Down = "Shift_Point_Down";
    public const string Shift_Point_Up = "Shift_Point_Up";

    // Shift point by half unit
    public const string Half_Shift_Point_Left = "Shift_Point_Left";
    public const string Half_Shift_Point_Right = "Shift_Point_Right";
    public const string Half_Shift_Point_Down = "Shift_Point_Down";
    public const string Half_Shift_Point_Up = "Shift_Point_Up";


    #endregion
}