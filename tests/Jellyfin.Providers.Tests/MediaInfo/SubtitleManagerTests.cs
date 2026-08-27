using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Subtitles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public sealed class SubtitleManagerTests
{
    [Fact]
    public async Task DownloadSubtitles_WithNonSeekableStream_SavesSubtitle()
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-subtitle-manager-tests-");
        try
        {
            var manager = CreateManager(new TestSubtitleProvider(new NonSeekableStream("hello")));
            var video = new Movie { Path = Path.Combine(tempDir.FullName, "Movie.mkv") };
            var subtitleId = await GetSubtitleId(manager);

            await manager.DownloadSubtitles(
                video,
                new LibraryOptions { SaveSubtitlesWithMedia = true },
                subtitleId,
                TestContext.Current.CancellationToken);

            Assert.Equal("hello", await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "Movie.en.srt"), TestContext.Current.CancellationToken));
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task DownloadSubtitles_WhenWriteFails_DeletesTemporaryFile()
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-subtitle-manager-tests-");
        try
        {
            var manager = CreateManager(new TestSubtitleProvider(new ThrowingReadStream()));
            var video = new Movie { Path = Path.Combine(tempDir.FullName, "Movie.mkv") };
            var subtitleId = await GetSubtitleId(manager);

            await Assert.ThrowsAnyAsync<Exception>(() => manager.DownloadSubtitles(
                video,
                new LibraryOptions { SaveSubtitlesWithMedia = true },
                subtitleId,
                TestContext.Current.CancellationToken));

            Assert.Empty(Directory.GetFiles(tempDir.FullName, "*.tmp"));
            Assert.False(File.Exists(Path.Combine(tempDir.FullName, "Movie.en.srt")));
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task DownloadSubtitles_WhenSubtitleExists_WritesNumberedSubtitle()
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-subtitle-manager-tests-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir.FullName, "Movie.en.srt"), "existing", TestContext.Current.CancellationToken);
            var manager = CreateManager(new TestSubtitleProvider(new NonSeekableStream("new subtitle")));
            var video = new Movie { Path = Path.Combine(tempDir.FullName, "Movie.mkv") };
            var subtitleId = await GetSubtitleId(manager);

            await manager.DownloadSubtitles(
                video,
                new LibraryOptions { SaveSubtitlesWithMedia = true },
                subtitleId,
                TestContext.Current.CancellationToken);

            Assert.Equal("existing", await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "Movie.en.srt"), TestContext.Current.CancellationToken));
            Assert.Equal("new subtitle", await File.ReadAllTextAsync(Path.Combine(tempDir.FullName, "Movie.en.0.srt"), TestContext.Current.CancellationToken));
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task DownloadSubtitles_WhenLanguageContainsPathSeparator_ThrowsAndWritesNothing()
    {
        var tempDir = Directory.CreateTempSubdirectory("jellyfin-subtitle-manager-tests-");
        try
        {
            var manager = CreateManager(new TestSubtitleProvider(new NonSeekableStream("hello"), "en/us"));
            var video = new Movie { Path = Path.Combine(tempDir.FullName, "Movie.mkv") };
            var subtitleId = await GetSubtitleId(manager);

            await Assert.ThrowsAsync<ArgumentException>(() => manager.DownloadSubtitles(
                video,
                new LibraryOptions { SaveSubtitlesWithMedia = true },
                subtitleId,
                TestContext.Current.CancellationToken));

            Assert.Empty(Directory.GetFiles(tempDir.FullName, "*.srt"));
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    private static SubtitleManager CreateManager(ISubtitleProvider subtitleProvider)
    {
        return new SubtitleManager(
            NullLogger<SubtitleManager>.Instance,
            Mock.Of<IFileSystem>(),
            Mock.Of<ILibraryMonitor>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<ILocalizationManager>(),
            [subtitleProvider],
            new NamingOptions());
    }

    private static async Task<string> GetSubtitleId(SubtitleManager manager)
    {
        var results = await manager.SearchSubtitles(
            new SubtitleSearchRequest
            {
                ContentType = VideoContentType.Movie,
                SearchAllProviders = true
            },
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        return Assert.Single(results).Id;
    }

    private sealed class TestSubtitleProvider(Stream stream, string language = "en") : ISubtitleProvider
    {
        public string Name => nameof(TestSubtitleProvider);

        public IEnumerable<VideoContentType> SupportedMediaTypes => [VideoContentType.Movie];

        public Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<RemoteSubtitleInfo>>([
                new RemoteSubtitleInfo
                {
                    Id = "subtitle",
                    ProviderName = Name,
                    Format = "srt",
                    ThreeLetterISOLanguageName = "eng"
                }
            ]);
        }

        public Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SubtitleResponse
            {
                Language = language,
                Format = "srt",
                Stream = stream
            });
        }
    }

    private sealed class NonSeekableStream(string content) : MemoryStream(Encoding.UTF8.GetBytes(content))
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("Read failed.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
