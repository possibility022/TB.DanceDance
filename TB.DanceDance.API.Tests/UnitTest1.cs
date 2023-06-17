using Azure.Storage.Blobs;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TB.DanceDance.API.Controllers;
using TB.DanceDance.API.Models;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class VideoControllerTests
    {

        VideoController controller = null!;
        Mock<IVideoService> videoService = null!;
        Mock<ITokenValidator> tokenValidator = null!;
        Mock<IUserService> userService = null!;
        Mock<BlobClient> blobClient = null!;

        const string userId = "userId";


        [TestInitialize]
        public void TestInit()
        {
            videoService = new();
            tokenValidator = new();
            userService = new();
            blobClient = new();

            controller = new VideoController(
                videoService.Object,
                tokenValidator.Object,
                userService.Object,
                NullLogger<VideoController>.Instance
                );

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "example name"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("sub", userId),
                new Claim("custom-claim", "example claim value"),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        public void TestCleanup()
        {
            try
            {
                controller?.Dispose();
            }
            catch
            {
                Console.WriteLine("Error on disposing.");
            }
        }

        [TestMethod]
        public async Task GetUploadInformation_ReturnUnauthorized_WhenUserIsNotAssignedToEvent()
        {
            var requestBody = new SharedVideoInformation()
            {
                NameOfVideo = "name",
                RecordedTimeUtc = DateTime.Now,
                SharedWith = Guid.NewGuid(),
                SharingWithType = SharingWithType.Event
            };

            userService.Setup(r => r.CanUserUploadToEventAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);

            var res = await controller.GetUploadInformation(requestBody);

            Assert.IsInstanceOfType(res.Result, typeof(UnauthorizedResult));

        }

        [TestMethod]
        public async Task GetUploadInformation_ReturnUnauthorized_WhenUserIsNotAssignedToGroup()
        {
            var requestBody = new SharedVideoInformation()
            {
                NameOfVideo = "name",
                RecordedTimeUtc = DateTime.Now,
                SharedWith = Guid.NewGuid(),
                SharingWithType = SharingWithType.Group
            };

            userService.Setup(r => r.CanUserUploadToGroupAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);

            var res = await controller.GetUploadInformation(requestBody);

            Assert.IsInstanceOfType(res.Result, typeof(UnauthorizedResult));

        }
    }
}