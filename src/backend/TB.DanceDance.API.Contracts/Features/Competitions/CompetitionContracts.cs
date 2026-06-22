using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Competitions
{
    /// <summary>Creates a new competition owned by the current user.</summary>
    public class CreateCompetitionRequest
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
        /// <summary>Who can see comments on the competition's combined thread. 0=AuthenticatedOnly, 1=OwnerOnly, 2=Public.</summary>
        public int CommentVisibility { get; set; }
    }

    /// <summary>Renames an existing competition.</summary>
    public class RenameCompetitionRequest
    {
        public string NewName { get; set; } = string.Empty;
    }

    /// <summary>A competition as seen in the owner's list, with a count of grouped videos.</summary>
    public class CompetitionSummaryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
        public int CommentVisibility { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int VideoCount { get; set; }
    }

    /// <summary>A single competition with the videos grouped into it.</summary>
    public class CompetitionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
        public int CommentVisibility { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public IReadOnlyCollection<VideoInformation> Videos { get; set; } = Array.Empty<VideoInformation>();
    }

    public class ListMyCompetitionsResponse
    {
        public IReadOnlyCollection<CompetitionSummaryResponse> Competitions { get; set; } = Array.Empty<CompetitionSummaryResponse>();
    }
}
