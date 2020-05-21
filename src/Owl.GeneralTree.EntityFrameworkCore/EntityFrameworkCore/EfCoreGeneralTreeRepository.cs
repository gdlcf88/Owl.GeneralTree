﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Owl.GeneralTree.EntityFrameworkCore
{
    public abstract class EfCoreGeneralTreeRepository<TDbContext, TTree, TPrimaryKey> :
        EfCoreRepository<TDbContext, TTree, TPrimaryKey>, IGeneralTreeRepository<TTree, TPrimaryKey>
        where TPrimaryKey : struct
        where TTree : class, IGeneralTree<TTree, TPrimaryKey>
        where TDbContext : IEfCoreDbContext
    {
        protected EfCoreGeneralTreeRepository(IDbContextProvider<TDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        public async Task<TTree> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await this.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        }

        public async Task<TTree> GetLastChildrenAsync(TPrimaryKey? parentId, CancellationToken cancellationToken = default)
        {
            return await this.Where(EqualParentId(parentId))
                .OrderByDescending(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<TTree>> GetChildrenAsync(TPrimaryKey? parentId, CancellationToken cancellationToken = default)
        {
            return await this.Where(EqualParentId(parentId)).ToListAsync(cancellationToken);
        }

        public async Task<List<TTree>> GetAllChildrenAsync(TPrimaryKey? parentId, CancellationToken cancellationToken = default)
        {
            if (parentId == null)
            {
                return await GetListAsync(cancellationToken: cancellationToken);
            }

            var tree = await GetAsync(parentId.Value, cancellationToken: cancellationToken);
            
            return await this.Where(x => x.Code.StartsWith(tree.Code))
                .Where(NotEqualId(parentId.Value))
                .ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<bool> CheckSameNameAsync(TPrimaryKey? parentId, string name, TPrimaryKey excludeId, CancellationToken cancellationToken = default)
        {
            return await this.Where(EqualParentId(parentId))
                .Where(NotEqualId(excludeId))
                .AnyAsync(x => x.Name == name, cancellationToken);
        }

        #region EqualExpression

        private static Expression<Func<TTree, bool>> EqualId(TPrimaryKey id)
        {
            var lambdaParam = Expression.Parameter(typeof(TTree));
            var leftExpression = Expression.PropertyOrField(lambdaParam, "Id");
            var rightExpression = Expression.Constant(id, typeof(TPrimaryKey));
            var lambdaBody = Expression.Equal(leftExpression, rightExpression);
            return Expression.Lambda<Func<TTree, bool>>(lambdaBody, lambdaParam);
        }

        private static Expression<Func<TTree, bool>> NotEqualId(TPrimaryKey id)
        {
            var lambdaParam = Expression.Parameter(typeof(TTree));
            var leftExpression = Expression.PropertyOrField(lambdaParam, "Id");
            var rightExpression = Expression.Constant(id, typeof(TPrimaryKey));
            var lambdaBody = Expression.NotEqual(leftExpression, rightExpression);
            return Expression.Lambda<Func<TTree, bool>>(lambdaBody, lambdaParam);
        }

        private static Expression<Func<TTree, bool>> EqualParentId(TPrimaryKey? parentId)
        {
            var lambdaParam = Expression.Parameter(typeof(TTree));
            var leftExpression = Expression.PropertyOrField(lambdaParam, "ParentId");
            var rightExpression = Expression.Constant(parentId, typeof(TPrimaryKey?));
            var lambdaBody = Expression.Equal(leftExpression, rightExpression);
            return Expression.Lambda<Func<TTree, bool>>(lambdaBody, lambdaParam);
        }

        private static Expression<Func<TTree, bool>> NotEqualParentId(TPrimaryKey? parentId)
        {
            var lambdaParam = Expression.Parameter(typeof(TTree));
            var leftExpression = Expression.PropertyOrField(lambdaParam, "ParentId");
            var rightExpression = Expression.Constant(parentId, typeof(TPrimaryKey?));
            var lambdaBody = Expression.NotEqual(leftExpression, rightExpression);
            return Expression.Lambda<Func<TTree, bool>>(lambdaBody, lambdaParam);
        }

        #endregion
    }
}
