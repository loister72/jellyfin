#nullable disable

#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.MediaEncoding;

public static class FfmpegReadRatePolicy
{
    public static string GetInputReadRateArguments(EncodingJobInfo state, EncodingOptions encodingOptions, Version ffmpegVersion)
    {
        var readRate = GetInputReadRate(state, encodingOptions, ffmpegVersion);
        if (readRate == 0)
        {
            return string.Empty;
        }

        if (state.ReadInputAtNativeFramerate && state.InputProtocol != MediaProtocol.Rtsp)
        {
            return GetCatchupArgument(" -re", readRate, ffmpegVersion);
        }

        return GetCatchupArgument($" -readrate {readRate}", readRate, ffmpegVersion);
    }

    private static int GetInputReadRate(EncodingJobInfo state, EncodingOptions encodingOptions, Version ffmpegVersion)
    {
        if (state.ReadInputAtNativeFramerate && state.InputProtocol != MediaProtocol.Rtsp)
        {
            return 1;
        }

        if (encodingOptions.EnableSegmentDeletion
            && state.VideoStream is not null
            && state.TranscodingType == TranscodingJobType.Hls
            && EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
            && ffmpegVersion >= FfmpegFeatureVersions.ReadrateOption)
        {
            // Limit HLS stream-copy reads so ffmpeg does not race ahead and exit before deleted segments are consumed.
            return 10;
        }

        return 0;
    }

    private static string GetCatchupArgument(string arguments, int readRate, Version ffmpegVersion)
    {
        if (ffmpegVersion < FfmpegFeatureVersions.ReadrateCatchupOption)
        {
            return arguments;
        }

        return $"{arguments} -readrate_catchup {readRate * 100}";
    }
}
