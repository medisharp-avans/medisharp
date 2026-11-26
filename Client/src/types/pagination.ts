/** One page of a longer list (Server.Models.DTO.Common.PagedResult). */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
