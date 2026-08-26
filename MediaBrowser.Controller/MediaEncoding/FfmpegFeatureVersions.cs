#nullable disable

#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.MediaEncoding;

public static class FfmpegFeatureVersions
{
    public static readonly Version ImplicitHwaccel = new(6, 0);

    public static readonly Version HwaUnsafeOutput = new(6, 0);

    public static readonly Version OclCuTonemapMode = new(5, 1, 3);

    public static readonly Version SvtAv1Params = new(5, 1);

    public static readonly Version VaapiH26xEncA53CcSei = new(6, 0);

    public static readonly Version ReadrateOption = new(5, 0);

    public static readonly Version WorkingVtHwSurface = new(7, 0, 1);

    public static readonly Version DisplayRotationOption = new(6, 0);

    public static readonly Version AdvancedTonemapMode = new(7, 0, 1);

    public static readonly Version AlteredVaVkInterop = new(7, 0, 1);

    public static readonly Version QsvVppTonemapOption = new(7, 0, 1);

    public static readonly Version QsvVppOutRangeOption = new(7, 0, 1);

    public static readonly Version VaapiDeviceVendorId = new(7, 0, 1);

    public static readonly Version QsvVppScaleModeOption = new(6, 0);

    public static readonly Version RkmppHevcDecDoviRpu = new(7, 1, 1);

    public static readonly Version ReadrateCatchupOption = new(8, 0);

    public static readonly Version NoiseBsfDrop = new(5, 0);
}
