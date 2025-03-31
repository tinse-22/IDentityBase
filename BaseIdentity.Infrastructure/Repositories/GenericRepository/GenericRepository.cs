using System.Linq.Expressions;
using BaseIdentity.Application.Interface.Repositories.IGenericRepository;
using BaseIdentity.Domain.Common;
using BaseIdentity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseIdentity.Infrastructure.Repositories.GenericRepository
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        private readonly IdentityBaseDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(IdentityBaseDbContext dbContext)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            // Lưu ý: Gọi SaveChangesAsync() thường sẽ được thực hiện từ UnitOfWork
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            if (includes != null && includes.Any())
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }

        public IQueryable<TEntity> GetQueryable()
        {
            // Nếu cần truy vấn phức tạp, nhưng hạn chế sử dụng trực tiếp ở tầng Application.
            return _dbSet.AsNoTracking();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (includes != null && includes.Any())
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // Giả sử tên thuộc tính định danh của entity là "Id"
            return await query.FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id").Equals(id));
        }

        public async Task<bool> UpdateAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            return true;
        }
    }
}
