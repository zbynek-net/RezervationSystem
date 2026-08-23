using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ReservationSystem.Repository;

namespace ReservationSystem.Tests.Fakes
{
    /// <summary>
    /// In-memory <see cref="IRepository"/> used by the unit tests.
    ///
    /// It deliberately mirrors the behaviour of the production EF-backed repository:
    /// <see cref="GetAll{TEntity}"/> and <see cref="Get{TEntity, TOrderBy}"/> return lazy iterator
    /// sequences (NOT <c>List</c>/<c>IList</c>), so callers that do <c>result as List&lt;T&gt;</c>
    /// get <c>null</c> and fall through to their own in-memory filtering - the same code path they
    /// hit against the real repository (whose <c>AsEnumerable()</c> wraps an EF query, not a list).
    /// </summary>
    public class FakeRepository : IRepository
    {
        private readonly Dictionary<Type, IList> _sets = new Dictionary<Type, IList>();

        public List<TEntity> Set<TEntity>() where TEntity : class
        {
            IList list;
            if (!_sets.TryGetValue(typeof(TEntity), out list))
            {
                list = new List<TEntity>();
                _sets[typeof(TEntity)] = list;
            }
            return (List<TEntity>)list;
        }

        /// <summary>Adds seed data for a given entity type. Returns this for fluent chaining.</summary>
        public FakeRepository Seed<TEntity>(params TEntity[] items) where TEntity : class
        {
            Set<TEntity>().AddRange(items);
            return this;
        }

        public void Add<TEntity>(IUnitOfWork uow, TEntity entity) where TEntity : class
        {
            if (entity == null) throw new ArgumentNullException("entity");
            Set<TEntity>().Add(entity);
        }

        public void Update<TEntity>(IUnitOfWork uow, TEntity entity) where TEntity : class
        {
            // Reference-based in-memory store: the caller already mutated the tracked instance.
        }

        public void Delete<TEntity>(IUnitOfWork uow, TEntity entity) where TEntity : class
        {
            if (entity == null) throw new ArgumentNullException("entity");
            Set<TEntity>().Remove(entity);
        }

        public IEnumerable<TEntity> GetAll<TEntity>(IUnitOfWork uow) where TEntity : class
        {
            // Iterator block => the returned object is not a List/IList (matches EF's AsEnumerable).
            foreach (var item in Set<TEntity>())
                yield return item;
        }

        public IQueryable<TEntity> GetQuery<TEntity>(IUnitOfWork uow) where TEntity : class
        {
            return Set<TEntity>().AsQueryable();
        }

        public IQueryable<TEntity> GetQuery<TEntity>(IUnitOfWork uow, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return Set<TEntity>().AsQueryable().Where(predicate);
        }

        public IEnumerable<TEntity> Get<TEntity, TOrderBy>(
            IUnitOfWork uow,
            Expression<Func<TEntity, bool>> criteria,
            Expression<Func<TEntity, TOrderBy>> orderBy,
            SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            var filtered = Set<TEntity>().Where(criteria.Compile());
            var ordered = sortOrder == SortOrder.Ascending
                ? filtered.OrderBy(orderBy.Compile())
                : filtered.OrderByDescending(orderBy.Compile());

            // Iterator block => not a List/IList, matching the production repository.
            foreach (var item in ordered)
                yield return item;
        }

        public TEntity GetByKey<TEntity>(IUnitOfWork uow, object keyValue) where TEntity : class
        {
            PropertyInfo idProp = typeof(TEntity).GetProperty("Id");
            if (idProp == null) return default(TEntity);
            return Set<TEntity>().FirstOrDefault(e => Equals(idProp.GetValue(e, null), keyValue));
        }
    }
}
