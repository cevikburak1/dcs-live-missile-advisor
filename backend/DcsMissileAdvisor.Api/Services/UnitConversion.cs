namespace DcsMissileAdvisor.Api.Services;

public static class UnitConversion
{
    public const double MpsToKnots = 1.94384;
    public const double MetersToFeet = 3.28084;
    public const double MetersToNm = 1.0 / 1852.0;
    public const double RadToDeg = 57.295779513;

    public static double? MpsToKnotsNullable(double? mps) =>
        mps.HasValue ? mps.Value * MpsToKnots : null;

    public static double? MetersToFeetNullable(double? meters) =>
        meters.HasValue ? meters.Value * MetersToFeet : null;

    public static double? MetersToNmNullable(double? meters) =>
        meters.HasValue ? meters.Value * MetersToNm : null;

    public static double? RadToDegNullable(double? rad) =>
        rad.HasValue ? rad.Value * RadToDeg : null;
}
