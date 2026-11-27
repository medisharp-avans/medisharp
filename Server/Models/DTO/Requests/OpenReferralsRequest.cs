using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests;

/// <summary>Identifies a page of open referrals, starting at page one.</summary>
public class OpenReferralsRequest
{
    public const int PageSize = 25;

    // Keep the calculated offset within the range EF Core accepts for Skip.
    [Range(1, int.MaxValue / PageSize, ErrorMessage = "Kies een geldig paginanummer.")]
    public int Page { get; set; } = 1;
}
