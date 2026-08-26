using System;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class FfmpegReadRatePolicyTests
{
    [Theory]
    [MemberData(nameof(ReadRateData))]
    public void GetInputReadRateArguments_AppliesExpectedPolicy(
        string name,
        EncodingJobInfo state,
        EncodingOptions encodingOptions,
        Version ffmpegVersion,
        string expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));

        var result = FfmpegReadRatePolicy.GetInputReadRateArguments(state, encodingOptions, ffmpegVersion);

        Assert.Equal(expected, result);
    }

    public static TheoryData<string, EncodingJobInfo, EncodingOptions, Version, string> ReadRateData()
    {
        return new TheoryData<string, EncodingJobInfo, EncodingOptions, Version, string>
        {
            {
                "Native framerate uses -re",
                CreateState(readInputAtNativeFramerate: true),
                new EncodingOptions(),
                new Version(7, 0),
                " -re"
            },
            {
                "Native framerate adds catchup on FFmpeg 8",
                CreateState(readInputAtNativeFramerate: true),
                new EncodingOptions(),
                new Version(8, 0),
                " -re -readrate_catchup 100"
            },
            {
                "RTSP native framerate does not apply readrate",
                CreateState(readInputAtNativeFramerate: true, inputProtocol: MediaProtocol.Rtsp),
                new EncodingOptions(),
                new Version(8, 0),
                string.Empty
            },
            {
                "HLS stream copy segment deletion uses readrate",
                CreateState(transcodingType: TranscodingJobType.Hls, outputVideoCodec: "copy", videoStream: new MediaStream()),
                new EncodingOptions { EnableSegmentDeletion = true },
                new Version(7, 0),
                " -readrate 10"
            },
            {
                "HLS stream copy segment deletion adds catchup on FFmpeg 8",
                CreateState(transcodingType: TranscodingJobType.Hls, outputVideoCodec: "copy", videoStream: new MediaStream()),
                new EncodingOptions { EnableSegmentDeletion = true },
                new Version(8, 0),
                " -readrate 10 -readrate_catchup 1000"
            },
            {
                "HLS stream copy segment deletion needs FFmpeg 5",
                CreateState(transcodingType: TranscodingJobType.Hls, outputVideoCodec: "copy", videoStream: new MediaStream()),
                new EncodingOptions { EnableSegmentDeletion = true },
                new Version(4, 4),
                string.Empty
            },
            {
                "Segment deletion does not rate limit transcoding",
                CreateState(transcodingType: TranscodingJobType.Hls, outputVideoCodec: "libx264", videoStream: new MediaStream()),
                new EncodingOptions { EnableSegmentDeletion = true },
                new Version(8, 0),
                string.Empty
            }
        };
    }

    private static EncodingJobInfo CreateState(
        bool readInputAtNativeFramerate = false,
        MediaProtocol inputProtocol = MediaProtocol.File,
        TranscodingJobType transcodingType = TranscodingJobType.Progressive,
        string? outputVideoCodec = null,
        MediaStream? videoStream = null)
    {
        return new EncodingJobInfo(transcodingType)
        {
            ReadInputAtNativeFramerate = readInputAtNativeFramerate,
            InputProtocol = inputProtocol,
            OutputVideoCodec = outputVideoCodec,
            VideoStream = videoStream
        };
    }
}
