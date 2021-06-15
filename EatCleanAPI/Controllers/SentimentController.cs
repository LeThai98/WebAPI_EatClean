using EatCleanAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EatCleanAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EatCleanAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SentimentController : Controller
    {
        private readonly VegafoodContext _context;

        public SentimentController(VegafoodContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IEnumerable<CustomerSentiment>> GetCategories()
        {
            return await _context.CustomerSentiments.ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerSentiment>> GetCategoriesById(int id)
        {
            var category = await _context.CustomerSentiments.Where(cus => cus.Id == id).FirstAsync();
            if (category == null)
                return NotFound();
            return category;
        }

        // POST api/<SentimentController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SentimentController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SentimentController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
