using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(string id);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);
        Task<Product> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(string id);
        Task<long> GetCountAsync();
    }
}
