using Azure.Storage.Blobs;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using TB.DanceDance.API.Controllers;
using TB.DanceDance.API.Models;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Tests
{
    [TestClass]
    public class VideoControllerTests
    {

        VideoController controller = null!;
        Mock<IVideoService> videoService = null!;
        Mock<ITokenValidator> tokenValidator = null!;
        Mock<IUserService> userService = null!;
        Mock<IVideoUploaderService> videoUploader = null!;
        Mock<BlobClient> blobClient = null!;

        const string userId = "userId";


        [TestInitialize]
        public void TestInit()
        {
            videoService = new();
            tokenValidator = new();
            userService = new();
            videoUploader = new();
            blobClient = new();

            controller = new VideoController(
                videoService.Object,
                tokenValidator.Object,
                userService.Object,
                videoUploader.Object,
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
        public async Task GetUploadInformation_PropertiesAreAssignedAsync()
        {
            var requestBody = new SharedVideoInformation()
            {
                NameOfVideo = "name",
                RecordedTimeUtc = DateTime.Now,
                SharedWith = new SharingScopeModel()
                {
                    Assignment = AssignmentType.Event,
                    Id = "123"
                }
            };

            userService.Setup(r => r.UserIsAssociatedWith(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            blobClient.SetupGet(r => r.Name)
                .Returns("BlobName");

            videoUploader.Setup(r => r.GetSasUri())
                .Returns(new Data.Blobs.SharedBlob()
                {
                    BlobClient = blobClient.Object,
                    Sas = new Uri("http://127.0.0.1/")
                });

            await controller.GetUploadInformation(requestBody);

            var uploadedBy = new SharingScope()
            {
                Assignment = AssignmentType.Person,
                EntityId = userId
            };

            videoService.Verify(r => r.SaveSharedVideoInformations(It.Is<SharedVideo>(v =>
                v.VideoInformation.Name == requestBody.NameOfVideo)), Times.Once);

            videoService.Verify(r => r.SaveSharedVideoInformations(It.Is<SharedVideo>(v =>
                v.VideoInformation.BlobId == "BlobName")), Times.Once);

            videoService.Verify(r => r.SaveSharedVideoInformations(It.Is<SharedVideo>(v =>
                v.VideoInformation.SharedWith.EntityId == requestBody.SharedWith.Id)), Times.Once);

            videoService.Verify(r => r.SaveSharedVideoInformations(It.Is<SharedVideo>(v =>
                v.VideoInformation.UploadedBy == uploadedBy)), Times.Once);

            videoService.Verify(r => r.SaveSharedVideoInformations(It.Is<SharedVideo>(v =>
                v.VideoInformation.SharedDateTimeUtc != default)), Times.Once);
        }

        [TestMethod]
        public async Task GetUploadInformation_ReturnUnauthorized_WhenUserIsNotAssignedToGroupOrEvent()
        {
            var requestBody = new SharedVideoInformation()
            {
                NameOfVideo = "name",
                RecordedTimeUtc = DateTime.Now,
                SharedWith = new SharingScopeModel()
                {
                    Assignment = AssignmentType.Event,
                    Id = "123"
                }
            };

            userService.Setup(r => r.UserIsAssociatedWith(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var res = await controller.GetUploadInformation(requestBody);

            Assert.IsInstanceOfType(res.Result, typeof(UnauthorizedResult));
            
        }
    }
}