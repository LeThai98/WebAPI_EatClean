using EatCleanAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : Controller
    {

        private readonly VegafoodContext _context;

        public HomeController(VegafoodContext vegafoodContext)
        {
            _context = vegafoodContext;
        }
        [HttpGet]
        public async Task<IEnumerable<Product>> GetProduct()
        {
            return await _context.Products.ToListAsync();

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products.Where(pro => pro.ProductId == id).FirstOrDefaultAsync();
            if (product == null)
                return NotFound($"Không tìm thấy Product");

            return product;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();

            return product;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest();
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Products.Any(pro => pro.ProductId == id) == false)
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct( int id)
        {
            var product = _context.Products.Where(pro => pro.ProductId == id).FirstOrDefault();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return product;
        }
    }
}
