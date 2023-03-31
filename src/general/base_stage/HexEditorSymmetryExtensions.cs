using System;

public static class HexEditorSymmetryExtensions
{
    public static int PositionCount(this HexEditorSymmetry symmetry)
    {
        switch (symmetry)
        {
            case HexEditorSymmetry.None:
                return 1;
            case HexEditorSymmetry.XAxisSymmetry:
                return 2;
            case HexEditorSymmetry.FourWaySymmetry:
                return 4;
            case HexEditorSymmetry.SixWaySymmetry:
                return 6;
            default:
                throw new ArgumentOutOfRangeException(nameof(symmetry), symmetry, null);
        }
    }
}
