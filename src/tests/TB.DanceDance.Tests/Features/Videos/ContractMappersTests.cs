using Application;

namespace TB.DanceDance.Tests.Features.Videos;

public class ContractMappersTests
{
    [Fact]
    public void MapToVideoInformation_MarksIsOwner_OnlyForTheUploader()
    {
        var video = new VideoDataBuilder().UploadedBy("user-1").Build();

        var asOwner = ContractMappers.MapToVideoInformation(video, thumbnailUrl: null, currentUserId: "user-1");
        var asOther = ContractMappers.MapToVideoInformation(video, thumbnailUrl: null, currentUserId: "user-2");
        var anonymous = ContractMappers.MapToVideoInformation(video, thumbnailUrl: null, currentUserId: null);

        Assert.True(asOwner.IsOwner);
        Assert.False(asOther.IsOwner);
        Assert.False(anonymous.IsOwner);
    }
}
