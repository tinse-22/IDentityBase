using System.Linq.Expressions;
using BaseIdentity.Domain.Common;

namespace BaseIdentity.Application.Interface.Repositories.IGenericRepository
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        Task<TEntity> AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task<IReadOnlyList<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includes);
        // Nếu không muốn tầng Application biết về IQueryable, cân nhắc hạn chế sử dụng phương thức này
        IQueryable<TEntity> GetQueryable();
        Task<TEntity?> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes);
        Task<bool> UpdateAsync(TEntity entity);
        Task<bool> DeleteAsync(TEntity entity);
    }
}
