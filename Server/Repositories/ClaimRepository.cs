using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class ClaimRepository(AppDbContext context) : Repository<Claim>(context), IClaimRepository
    {
        /// <inheritdoc />
        public async Task<(List<ClaimOverviewDTO> Items, int TotalCount)> GetOverviewPageAsync(PagedQuery query, ClaimStatus? status = null)
        {
            var claims = _context.Claims.AsNoTracking();
            if (status is { } wanted) claims = claims.Where(c => c.Status == wanted);

            var rows = ApplySearch(ToOverview(claims), query.Search);

            var totalCount = await rows.CountAsync();
            var items = await ApplySort(rows, query.SortBy, query.SortDescending)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc />
        public Task<int> CountToReviewAsync() =>
            _context.Claims.CountAsync(c => c.Status == ClaimStatus.ToReview);

        /// <summary>One overview row per claim, with the number of lines, the total and the reviewer.</summary>
        private static IQueryable<ClaimOverviewDTO> ToOverview(IQueryable<Claim> claims) =>
            claims.Select(c => new ClaimOverviewDTO
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = c.Patient.User.FullName,
                PolicyNumber = c.PolicyNumber,
                Status = c.Status,
                LineCount = c.ClaimLines.Count,
                TotalAmount = c.ClaimLines.Sum(l => l.SubmittedAmount),
                ReviewedByName = c.ReviewedBy != null ? c.ReviewedBy.User.FullName : null,
                CreatedAt = c.CreatedAt,
                SubmittedAt = c.SubmittedAt,
                LastModifiedAt = c.LastModifiedAt
            });

        /// <summary>Keeps the rows whose claim number, patient, policy number or reviewer matches the search term.</summary>
        private static IQueryable<ClaimOverviewDTO> ApplySearch(IQueryable<ClaimOverviewDTO> rows, string? search)
        {
            var term = search?.Trim().TrimStart('#');
            if (string.IsNullOrEmpty(term)) return rows;

            var isNumber = int.TryParse(term, out var claimId);

            return rows.Where(r =>
                (isNumber && r.Id == claimId) ||
                r.PatientName.Contains(term) ||
                r.PolicyNumber.Contains(term) ||
                (r.ReviewedByName != null && r.ReviewedByName.Contains(term)));
        }

        /// <summary>
        /// Sorts on the chosen column (case-insensitive). Without a known column: claims to review first,
        /// then the most recently changed. The claim number is always the last tie-breaker, so paging is stable.
        /// </summary>
        private static IQueryable<ClaimOverviewDTO> ApplySort(IQueryable<ClaimOverviewDTO> rows, string? sortBy, bool descending)
        {
            IOrderedQueryable<ClaimOverviewDTO> sorted = sortBy?.ToLowerInvariant() switch
            {
                "id" => Order(rows, r => r.Id, descending),
                "patient" => Order(rows, r => r.PatientName, descending),
                "policynumber" => Order(rows, r => r.PolicyNumber, descending),
                // The status is stored as text, so sort on the workflow order instead of the alphabet.
                "status" => Order(rows, r =>
                    r.Status == ClaimStatus.ToReview ? 0 :
                    r.Status == ClaimStatus.ReadyToSend ? 1 :
                    r.Status == ClaimStatus.Processed ? 2 : 3, descending),
                "linecount" => Order(rows, r => r.LineCount, descending),
                "totalamount" => Order(rows, r => r.TotalAmount, descending),
                "reviewedby" => Order(rows, r => r.ReviewedByName, descending),
                "createdat" => Order(rows, r => r.CreatedAt, descending),
                "lastmodifiedat" => Order(rows, r => r.LastModifiedAt, descending),
                _ => rows
                    .OrderBy(r => r.Status == ClaimStatus.ToReview ? 0 : 1)
                    .ThenByDescending(r => r.LastModifiedAt)
            };

            return sorted.ThenBy(r => r.Id);
        }

        private static IOrderedQueryable<ClaimOverviewDTO> Order<TKey>(
            IQueryable<ClaimOverviewDTO> rows,
            Expression<Func<ClaimOverviewDTO, TKey>> key,
            bool descending) =>
            descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
    }
}
