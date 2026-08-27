using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.LiveTv.TunerHosts;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class M3UTunerHostTests
    {
        private readonly TestM3UTunerHost _m3uTunerHost;

        public M3UTunerHostTests()
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns<string>(path => path.StartsWith("udp", System.StringComparison.OrdinalIgnoreCase)
                    ? MediaProtocol.Udp
                    : MediaProtocol.Http);

            var networkManager = new Mock<INetworkManager>();
            networkManager
                .Setup(x => x.IsInLocalNetwork(It.IsAny<string>()))
                .Returns(false);

            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization
            {
                ConfigureMembers = true
            });
            fixture.Inject(mediaSourceManager.Object);
            fixture.Inject(networkManager.Object);
            fixture.Inject(Mock.Of<IHttpClientFactory>());

            _m3uTunerHost = new TestM3UTunerHost(
                fixture.Create<IServerConfigurationManager>(),
                mediaSourceManager.Object,
                fixture.Create<Microsoft.Extensions.Logging.ILogger<M3UTunerHost>>(),
                fixture.Create<IFileSystem>(),
                fixture.Create<IHttpClientFactory>(),
                fixture.Create<MediaBrowser.Controller.IServerApplicationHost>(),
                networkManager.Object,
                fixture.Create<IStreamHelper>());
        }

        [Theory]
        [InlineData("http://example.com/live/channel.ts", MediaProtocol.Http, "ts")]
        [InlineData("https://example.com/live/channel.m3u8?token=abc", MediaProtocol.Http, "m3u8")]
        [InlineData("udp://239.0.0.1:1234", MediaProtocol.Udp, null)]
        public void CreateMediaSourceInfo_NormalizesLiveStreamContract(string path, MediaProtocol protocol, string? container)
        {
            var mediaSource = _m3uTunerHost.CreateMediaSourceInfo(
                new TunerHostInfo(),
                new ChannelInfo
                {
                    Path = path
                });

            Assert.Equal(path, mediaSource.Path);
            Assert.Equal(protocol, mediaSource.Protocol);
            Assert.Equal(container, mediaSource.Container);
            Assert.Equal(0, mediaSource.BufferMs);
            Assert.True(mediaSource.RequiresOpening);
            Assert.True(mediaSource.RequiresClosing);
            Assert.True(mediaSource.IsInfiniteStream);
            Assert.True(mediaSource.SupportsTranscoding);
        }

        [Theory]
        [InlineData("http://example.com/live/channel.m3u", "m3u8")]
        [InlineData("http://example.com/live/channel.m3u8?token=abc", "m3u8")]
        [InlineData("http://example.com/live/channel.mpegts", "ts")]
        [InlineData("http://example.com/live/channel.tsv", "ts")]
        [InlineData("http://example.com/live/channel", null)]
        public void GetContainerFromPath_UsesStablePathExtensions(string path, string? container)
        {
            Assert.Equal(container, M3UTunerHost.GetContainerFromPath(path));
        }

        private sealed class TestM3UTunerHost : M3UTunerHost
        {
            public TestM3UTunerHost(
                IServerConfigurationManager config,
                IMediaSourceManager mediaSourceManager,
                Microsoft.Extensions.Logging.ILogger<M3UTunerHost> logger,
                IFileSystem fileSystem,
                IHttpClientFactory httpClientFactory,
                MediaBrowser.Controller.IServerApplicationHost appHost,
                INetworkManager networkManager,
                IStreamHelper streamHelper)
                : base(config, mediaSourceManager, logger, fileSystem, httpClientFactory, appHost, networkManager, streamHelper)
            {
            }

            public new MediaSourceInfo CreateMediaSourceInfo(TunerHostInfo info, ChannelInfo channel)
                => base.CreateMediaSourceInfo(info, channel);
        }
    }
}
