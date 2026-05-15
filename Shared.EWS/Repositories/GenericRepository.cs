using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Infrastructure.EWS.Repositories
{
    public class GenericRepository<T>(EWSDbContext context) : IGenericRepository<T>
        where T : class
    {
        protected readonly EWSDbContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public async Task<T?> GetByIdAsync(object id)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity is BaseEntity baseEntity && baseEntity.IsDeleted)
                return null;

            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                return await _dbSet
                    .Where(e => !EF.Property<bool>(e, nameof(BaseEntity.IsDeleted)))
                    .AsNoTracking()
                    .ToListAsync();
            }

            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.CreatedAt = DateTime.UtcNow;
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            if (entity is BaseEntity baseEntity)
                baseEntity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(object id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(entity);
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}