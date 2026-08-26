using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class SessionManagerTests
{
    [Theory]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(null, typeof(ArgumentNullException))]
    public async Task GetAuthorizationToken_Should_ThrowException(string? deviceId, Type exceptionType)
    {
        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());

        await Assert.ThrowsAsync(exceptionType, () => sessionManager.GetAuthorizationToken(
            new User("test", "default", "default"),
            deviceId,
            "app_name",
            "0.0.0",
            "device_name"));
    }

    [Theory]
    [MemberData(nameof(AuthenticateNewSessionInternal_Exception_TestData))]
    public async Task AuthenticateNewSessionInternal_Should_ThrowException(AuthenticationRequest authenticationRequest, Type exceptionType)
    {
        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());

        await Assert.ThrowsAsync(exceptionType, () => sessionManager.AuthenticateNewSessionInternal(authenticationRequest, false));
    }

    public static TheoryData<AuthenticationRequest, Type> AuthenticateNewSessionInternal_Exception_TestData()
    {
        var data = new TheoryData<AuthenticationRequest, Type>
        {
            {
                new AuthenticationRequest { App = string.Empty, DeviceId = "device_id", DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = null, DeviceId = "device_id", DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = string.Empty, DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = null, DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = string.Empty, AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = null, AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = "device_name", AppVersion = string.Empty },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = "device_name", AppVersion = null },
                typeof(ArgumentNullException)
            }
        };

        return data;
    }

    [Fact]
    public async Task SendGeneralCommand_WhenControllingOwnSession_AllowsCommand()
    {
        var user = CreateUser("user");
        await using var sessionManager = CreateSessionManager(user);
        var session = await CreateSession(sessionManager, "device", user);

        await sessionManager.SendGeneralCommand(session.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendGeneralCommand_WhenControllingAdditionalUserSession_AllowsCommand()
    {
        var primaryUser = CreateUser("primary");
        var additionalUser = CreateUser("additional");
        await using var sessionManager = CreateSessionManager(primaryUser, additionalUser);
        var session = await CreateSession(sessionManager, "target-device", primaryUser);
        session.AdditionalUsers = [new SessionUserInfo { UserId = additionalUser.Id, UserName = additionalUser.Username }];
        var controllingSession = await CreateSession(sessionManager, "controller-device", additionalUser);

        await sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendGeneralCommand_WhenControllerCannotControlOthers_ThrowsSecurityException()
    {
        var targetUser = CreateUser("target");
        var controllingUser = CreateUser("controller");
        await using var sessionManager = CreateSessionManager(targetUser, controllingUser);
        var session = await CreateSession(sessionManager, "target-device", targetUser);
        var controllingSession = await CreateSession(sessionManager, "controller-device", controllingUser);

        await Assert.ThrowsAsync<SecurityException>(() =>
            sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendGeneralCommand_WhenTargetDoesNotSupportRemoteControl_ThrowsSecurityException()
    {
        var targetUser = CreateUser("target");
        var controllingUser = CreateUser("controller");
        controllingUser.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        await using var sessionManager = CreateSessionManager(targetUser, controllingUser);
        var session = await sessionManager.LogSessionActivity("app", "1.0", "target-device", "target-device", "127.0.0.1", targetUser);
        var controllingSession = await CreateSession(sessionManager, "controller-device", controllingUser);

        await Assert.ThrowsAsync<SecurityException>(() =>
            sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendGeneralCommand_WhenTargetDisablesSharedDeviceControl_ThrowsSecurityException()
    {
        var targetUser = CreateUser("target");
        targetUser.SetPermission(PermissionKind.EnableSharedDeviceControl, false);
        var controllingUser = CreateUser("controller");
        controllingUser.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        await using var sessionManager = CreateSessionManager(targetUser, controllingUser);
        var session = await CreateSession(sessionManager, "target-device", targetUser);
        var controllingSession = await CreateSession(sessionManager, "controller-device", controllingUser);

        await Assert.ThrowsAsync<SecurityException>(() =>
            sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendGeneralCommand_WhenControllerCanControlOthersAndTargetSharesDevice_AllowsCommand()
    {
        var targetUser = CreateUser("target");
        var controllingUser = CreateUser("controller");
        controllingUser.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        await using var sessionManager = CreateSessionManager(targetUser, controllingUser);
        var session = await CreateSession(sessionManager, "target-device", targetUser);
        var controllingSession = await CreateSession(sessionManager, "controller-device", controllingUser);

        await sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendGeneralCommand_WhenControllerCannotAccessTargetDevice_ThrowsSecurityException()
    {
        var targetUser = CreateUser("target");
        var controllingUser = CreateUser("controller");
        controllingUser.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(i => i.CanAccessDevice(controllingUser, "blocked-device")).Returns(false);
        await using var sessionManager = CreateSessionManager(deviceManager.Object, targetUser, controllingUser);
        var session = await CreateSession(sessionManager, "blocked-device", targetUser);
        var controllingSession = await CreateSession(sessionManager, "controller-device", controllingUser);

        await Assert.ThrowsAsync<SecurityException>(() =>
            sessionManager.SendGeneralCommand(controllingSession.Id, session.Id, new GeneralCommand(), TestContext.Current.CancellationToken));
    }

    private static User CreateUser(string username)
    {
        var user = new User(username, "default", "default");
        user.AddDefaultPermissions();
        return user;
    }

    private static Task<SessionInfo> CreateSession(
        Emby.Server.Implementations.Session.SessionManager sessionManager,
        string deviceId,
        User user)
    {
        return CreateSessionInternal(sessionManager, deviceId, user);
    }

    private static async Task<SessionInfo> CreateSessionInternal(
        Emby.Server.Implementations.Session.SessionManager sessionManager,
        string deviceId,
        User user)
    {
        var session = await sessionManager.LogSessionActivity("app", "1.0", deviceId, deviceId, "127.0.0.1", user);
        session.Capabilities = new ClientCapabilities
        {
            SupportsMediaControl = true
        };

        var controller = new Mock<ISessionController>();
        controller.Setup(i => i.SupportsMediaControl).Returns(true);
        controller.Setup(i => i.IsSessionActive).Returns(true);
        controller.Setup(i => i.SendMessage(It.IsAny<SessionMessageType>(), It.IsAny<Guid>(), It.IsAny<GeneralCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        session.AddController(controller.Object);
        return session;
    }

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(params User[] users)
    {
        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(i => i.CanAccessDevice(It.IsAny<User>(), It.IsAny<string>())).Returns(true);

        return CreateSessionManager(deviceManager.Object, users);
    }

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(IDeviceManager deviceManager, params User[] users)
    {
        var userManager = new Mock<IUserManager>();
        foreach (var user in users)
        {
            userManager.Setup(i => i.GetUserById(user.Id)).Returns(user);
        }

        return new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            userManager.Object,
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            deviceManager,
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());
    }
}
