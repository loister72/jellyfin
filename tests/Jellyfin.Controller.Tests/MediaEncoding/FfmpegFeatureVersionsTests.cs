using System;
using MediaBrowser.Controller.MediaEncoding;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class FfmpegFeatureVersionsTests
{
    [Theory]
    [MemberData(nameof(FeatureVersionData))]
    public void FeatureVersionCatalog_UsesExpectedMinimumVersions(string name, Version actual, Version expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, Version, Version> FeatureVersionData()
    {
        return new TheoryData<string, Version, Version>
        {
            { nameof(FfmpegFeatureVersions.ImplicitHwaccel), FfmpegFeatureVersions.ImplicitHwaccel, new Version(6, 0) },
            { nameof(FfmpegFeatureVersions.HwaUnsafeOutput), FfmpegFeatureVersions.HwaUnsafeOutput, new Version(6, 0) },
            { nameof(FfmpegFeatureVersions.OclCuTonemapMode), FfmpegFeatureVersions.OclCuTonemapMode, new Version(5, 1, 3) },
            { nameof(FfmpegFeatureVersions.SvtAv1Params), FfmpegFeatureVersions.SvtAv1Params, new Version(5, 1) },
            { nameof(FfmpegFeatureVersions.VaapiH26xEncA53CcSei), FfmpegFeatureVersions.VaapiH26xEncA53CcSei, new Version(6, 0) },
            { nameof(FfmpegFeatureVersions.ReadrateOption), FfmpegFeatureVersions.ReadrateOption, new Version(5, 0) },
            { nameof(FfmpegFeatureVersions.WorkingVtHwSurface), FfmpegFeatureVersions.WorkingVtHwSurface, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.DisplayRotationOption), FfmpegFeatureVersions.DisplayRotationOption, new Version(6, 0) },
            { nameof(FfmpegFeatureVersions.AdvancedTonemapMode), FfmpegFeatureVersions.AdvancedTonemapMode, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.AlteredVaVkInterop), FfmpegFeatureVersions.AlteredVaVkInterop, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.QsvVppTonemapOption), FfmpegFeatureVersions.QsvVppTonemapOption, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.QsvVppOutRangeOption), FfmpegFeatureVersions.QsvVppOutRangeOption, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.VaapiDeviceVendorId), FfmpegFeatureVersions.VaapiDeviceVendorId, new Version(7, 0, 1) },
            { nameof(FfmpegFeatureVersions.QsvVppScaleModeOption), FfmpegFeatureVersions.QsvVppScaleModeOption, new Version(6, 0) },
            { nameof(FfmpegFeatureVersions.RkmppHevcDecDoviRpu), FfmpegFeatureVersions.RkmppHevcDecDoviRpu, new Version(7, 1, 1) },
            { nameof(FfmpegFeatureVersions.ReadrateCatchupOption), FfmpegFeatureVersions.ReadrateCatchupOption, new Version(8, 0) },
            { nameof(FfmpegFeatureVersions.NoiseBsfDrop), FfmpegFeatureVersions.NoiseBsfDrop, new Version(5, 0) }
        };
    }
}
