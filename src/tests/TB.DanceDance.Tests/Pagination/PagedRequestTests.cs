using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.Tests.Pagination;

public class PagedRequestTests
{
    [Fact]
    public void Defaults_AreFirstPage_WithDefaultPageSize()
    {
        var request = new PagedRequest();

        Assert.Equal(1, request.NormalizedPage);
        Assert.Equal(PagedRequest.DefaultPageSize, request.NormalizedPageSize);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void NormalizedPage_ClampsToAtLeastOne(int page, int expected)
    {
        var request = new PagedRequest { Page = page };

        Assert.Equal(expected, request.NormalizedPage);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(50, 50)]
    [InlineData(PagedRequest.MaxPageSize, PagedRequest.MaxPageSize)]
    [InlineData(PagedRequest.MaxPageSize + 1, PagedRequest.MaxPageSize)]
    public void NormalizedPageSize_ClampsToValidRange(int pageSize, int expected)
    {
        var request = new PagedRequest { PageSize = pageSize };

        Assert.Equal(expected, request.NormalizedPageSize);
    }
}
