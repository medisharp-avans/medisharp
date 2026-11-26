using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Common
{
    /// <summary>
    /// Search, sort and paging options for any overview page. Every option is optional.
    /// Which columns <see cref="SortBy"/> accepts is up to the repository of that overview.
    /// </summary>
    public class PagedQuery
    {
        [StringLength(100, ErrorMessage = "De zoekterm mag maximaal 100 tekens zijn.")]
        public string? Search { get; set; }

        /// <summary>The column to sort on, for example "Patient". Unknown or empty: the default order of the overview.</summary>
        [StringLength(50)]
        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "De pagina moet 1 of hoger zijn.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Het aantal per pagina moet tussen 1 en 100 liggen.")]
        public int PageSize { get; set; } = 20;
    }
}
