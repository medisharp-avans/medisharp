namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Defines a generic repository interface for standard CRUD operations on entity type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type this repository operates on, restricted to classes.</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves an entity by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>The matching entity if found; otherwise, <c>null</c>.</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all entities of type <typeparamref name="T"/> asynchronously.
        /// </summary>
        /// <returns>A list containing all entities.</returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Adds a new entity to the repository context asynchronously.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        Task AddAsync(T entity);

        /// <summary>
        /// Marks an existing entity as modified within the repository context.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        void Update(T entity);

        /// <summary>
        /// Marks an existing entity for deletion within the repository context.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        void Delete(T entity);

        /// <summary>
        /// Persists all pending changes made within the context to the underlying database asynchronously.
        /// </summary>
        Task SaveChangesAsync();
    }
}