using Domain.CachingService;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductRepository productRepository,
            ICacheInvalidationService cacheInvalidation,
            ILogger<ProductsController> logger)
        {
            _productRepository = productRepository;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        /// <summary>
        /// Get all products (cached with FusionCache L1+L2)
        /// Cache Duration: 5 minutes
        /// Fail-Safe: Returns stale data if MongoDB is down
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            _logger.LogInformation("Getting all products");
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        /// <summary>
        /// Get product by ID (cached with high priority)
        /// Cache Duration: 10 minutes
        /// Features: Stampede protection, Fail-Safe, Soft timeout 500ms
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(string id)
        {
            _logger.LogInformation("Getting product by ID: {ProductId}", id);

            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }

        /// <summary>
        /// Get products by category (cached per category)
        /// Cache Duration: 7 minutes
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetByCategory(string category)
        {
            _logger.LogInformation("Getting products by category: {Category}", category);
            var products = await _productRepository.GetByCategoryAsync(category);
            return Ok(products);
        }

        /// <summary>
        /// Search products (cached with low priority)
        /// Cache Duration: 3 minutes (shorter for search results)
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Search query cannot be empty" });
            }

            _logger.LogInformation("Searching products: {Query}", q);
            var products = await _productRepository.SearchAsync(q);
            return Ok(products);
        }

        /// <summary>
        /// Get product count (cached)
        /// Cache Duration: 2 minutes
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<object>> GetCount()
        {
            _logger.LogInformation("Getting product count");
            var count = await _productRepository.GetCountAsync();
            return Ok(new { count });
        }

        /// <summary>
        /// Create product (invalidates related caches automatically)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Product>> Create([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating product: {ProductName}", product.Name);
            var created = await _productRepository.CreateAsync(product);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        /// <summary>
        /// Update product (invalidates related caches automatically)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Product product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating product: {ProductId}", id);
            var updated = await _productRepository.UpdateAsync(product);

            if (!updated)
            {
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            return NoContent();
        }

        /// <summary>
        /// Delete product (soft delete, invalidates caches)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation("Deleting product: {ProductId}", id);
            var deleted = await _productRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            return NoContent();
        }
    }
}
