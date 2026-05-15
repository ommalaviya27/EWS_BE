using Shared.EWS.Interfaces;
using Shared.EWS.Interfaces.Repositories;

namespace Shared.EWS.Services
{
    public class GenericService<TEntity>(IGenericRepository<TEntity> repository) : IGenericService<TEntity>
        where TEntity : class
    {
        protected readonly IGenericRepository<TEntity> _repository = repository;

        public virtual Task<TEntity?> GetByIdAsync(object id)
            => _repository.GetByIdAsync(id);
 
        public virtual Task<IEnumerable<TEntity>> GetAllAsync()
            => _repository.GetAllAsync();
 
        public virtual Task<TEntity> AddAsync(TEntity entity)
            => _repository.AddAsync(entity);
 
        public virtual Task<TEntity> UpdateAsync(TEntity entity)
            => _repository.UpdateAsync(entity);
 
        public virtual Task<bool> DeleteAsync(object id)
            => _repository.DeleteAsync(id);
    }
}