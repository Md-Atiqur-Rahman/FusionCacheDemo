using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Direct MongoDB repository without caching
    /// Uses Repository Pattern for data access abstraction
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<Product> _collection;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(
            MongoDbContext context,
            ILogger<ProductRepository> logger)
        {
            _collection = context.GetCollection<Product>("products");
            _logger = logger;

            // Create indexes for performance
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var categoryIndex = Builders<Product>.IndexKeys.Ascending(p => p.Category);
            var nameIndex = Builders<Product>.IndexKeys.Text(p => p.Name);
            var isActiveIndex = Builders<Product>.IndexKeys.Ascending(p => p.IsActive);

            _collection.Indexes.CreateOne(new CreateIndexModel<Product>(categoryIndex));
            _collection.Indexes.CreateOne(new CreateIndexModel<Product>(nameIndex));
            _collection.Indexes.CreateOne(new CreateIndexModel<Product>(isActiveIndex));
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            try
            {
                var filter = Builders<Product>.Filter.And(
                    Builders<Product>.Filter.Eq(p => p.Id, id),
                    Builders<Product>.Filter.Eq(p => p.IsActive, true)
                );

                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            try
            {
                var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            try
            {
                var filter = Builders<Product>.Filter.And(
                    Builders<Product>.Filter.Eq(p => p.Category, category),
                    Builders<Product>.Filter.Eq(p => p.IsActive, true)
                );

                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            try
            {
                var filter = Builders<Product>.Filter.And(
                    Builders<Product>.Filter.Text(searchTerm),
                    Builders<Product>.Filter.Eq(p => p.IsActive, true)
                );

                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<Product> CreateAsync(Product product)
        {
            try
            {
                product.CreatedAt = DateTime.UtcNow;
                await _collection.InsertOneAsync(product);

                _logger.LogInformation("Product created: {ProductId}", product.Id);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            try
            {
                product.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
                var result = await _collection.ReplaceOneAsync(filter, product);

                var success = result.ModifiedCount > 0;

                if (success)
                {
                    _logger.LogInformation("Product updated: {ProductId}", product.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", product.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                // Soft delete - just mark as inactive
                var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
                var update = Builders<Product>.Update
                    .Set(p => p.IsActive, false)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                var result = await _collection.UpdateOneAsync(filter, update);

                var success = result.ModifiedCount > 0;

                if (success)
                {
                    _logger.LogInformation("Product deleted (soft): {ProductId}", id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                throw;
            }
        }

        public async Task<long> GetCountAsync()
        {
            try
            {
                var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);
                return await _collection.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product count");
                throw;
            }
        }
    }
}
