using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatCleanAPI.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using EatCleanAPI.Catalog.Services;
using EatCleanAPI.ViewModels;
using EatCleanAPI.Catalog.Common;

namespace EatCleanAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {   
      
        private readonly VegafoodContext _context;
        private readonly JWTSettings _jwtsettings;
        private readonly IUserService _userService;

        public CustomersController(VegafoodContext context, IOptions<JWTSettings> jwtsettings, IUserService userService)
        {
            _context = context;

            // ta lấy secretkey từ file appsettings gán vào field Secretkey của tham số jwtsettings. 
            _jwtsettings = jwtsettings.Value;

            _userService = userService;


            // string t= _jwtsettings.SecretKey;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers.ToListAsync();

        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            //EntityEntry
            //SqlServerDbContextOptionsExtensions
            // Khi ta có 1 đối tượng là Customer, ta sẽ truyền đối tượng đó vào tham số cus trong biểu thức lambda, 
            // biểu thức này dạng Func và trả ra đó là 1 đối tượng Order là 1 thuộc tính trong Customer
            var customer = await _context.Customers
                                         .Include(cus => cus.Orders)
                                         .Where(cus => cus.CustomerId == id).FirstOrDefaultAsync();

            //var customer = await _context.Customers.SingleAsync(cus => cus.CustomerId == id);
            int a;
           

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

      
        [HttpGet("GetCustomerDetail/{id}")]
        public async Task<ActionResult<Customer>> GetCustomerDetail( int id)
        {
            var customer = await  _context.Customers.SingleAsync(cus => cus.CustomerId == id);

           // var cus1 = await _context.Customers.AllAsync(cus => cus.City == "Hồ Chí Minh");
            
            _context.Entry(customer)
                .Collection(cus => cus.Orders)
                .Query()
                .Include(order => order.OrderDetails)
                .Load();

           
            return customer;

          
        }

        //[HttpPost("SignIn")]
        //public async Task<ActionResult<Customer>> Login([FromBody] Customer customer)
        //{
        //    // tìm ra customer trong db
        //    customer = await _context.Customers.
        //        Where(cus => cus.Email == customer.Email && cus.Password == customer.Password)
        //        .FirstOrDefaultAsync();

        //    // tạo đối tượng CustomerWithToken
        //    CustomerWithToken customerWithToken = new CustomerWithToken(customer);

        //    // nếu có customer trong db
        //    if (customer != null)
        //    {
        //        // tạo ra refreshToken 
        //        RefreshToken refreshToken = GenerateRefreshToken();
        //        // add refreshtoken cho customer 
        //        customer.RefreshTokens.Add(refreshToken);
        //        // save việc insert đối tượng vào trong csdl 
        //        await _context.SaveChangesAsync();

        //        // tạo 1 đối tượng customerwithToken truyền vào là customer đã có refreshtoken
        //        customerWithToken = new CustomerWithToken(customer);
        //        // đối tượng customerWithToken đc đc refreshtoken
        //        customerWithToken.RefreshToken = refreshToken.Token;
        //    }

        //    if (customerWithToken == null)
        //    {
        //        return NotFound();
        //    }


        //    //    //sign your token here here..
        //    // tạo access token cho customer
        //       customerWithToken.AccessToken = GenerateAccessToken(customer.CustomerId);

        //    //    //var tokerHandler = new JwtSecurityTokenHandler();
        //    //    //var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
        //    //    //var tokenDescriptor = new SecurityTokenDescriptor
        //    //    //{
        //    //    //    Subject = new ClaimsIdentity(new Claim[]
        //    //    //    {
        //    //    //        new Claim(ClaimTypes.Name, Convert.ToString( customer.CustomerId))
        //    //    //    }),
        //    //    //    Expires = DateTime.UtcNow.AddMonths(6),
        //    //    //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
        //    //    //    SecurityAlgorithms.HmacSha256Signature)
        //    //    //};
        //    //    //var token = tokerHandler.CreateToken(tokenDescriptor);
        //    //    //customerWithToken.AccessToken = tokerHandler.WriteToken(token);

        //     return customerWithToken;

        // }

        [HttpPost("RefreshToken")]
        public async Task<ActionResult<Customer>> RefreshToken([FromBody] RefreshRequest refreshRequest)
        {
            // tạo 1 customer new từ accesstoken, customer có thể là null( tạo mới), hoặc là tìm ra đc customer đang giữ accestoken đó
            Customer customer = await GetUserFromAccessToken(refreshRequest.AccessToken);

            // trong customer hiện tại sẽ có refresh token, lấy refresh token đó đi kiểm tra
            // thực hiện Check customer và validate refreshtoken
            if (customer != null && ValidateRefreshToken(customer, refreshRequest.RefreshToken))
            {
                // create 1 customer có refreshtoken
                CustomerWithToken customerWithToken = new CustomerWithToken(customer);
                // cấp lại access token cho customer
                customerWithToken.AccessToken = GenerateAccessToken(customer.CustomerId);
                customerWithToken.RefreshToken = refreshRequest.RefreshToken;
                return customerWithToken;
            }

            return null;
        }

        private bool ValidateRefreshToken(Customer customer, string refreshToken)
        {
            // lấy ra đối tượng refreshtoken trong DB từ chuỗi refreshtoken từ parameter truyền vào , lấy ra token có expiry lớn nhất
            RefreshToken refreshTokenUser = _context.RefreshTokens.Where(rt => rt.Token == refreshToken)
                                                .OrderByDescending(rt => rt.ExpiryDate)
                                                .FirstOrDefault();
            // kiểm tra refreshtoken có null hay không, có customerid có bằng với token của user trong parameter hay không, có còn hạn sử dụng hay không
            if (refreshTokenUser != null && refreshTokenUser.CustomerId == customer.CustomerId
                && refreshTokenUser.ExpiryDate > DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }


        private async Task<Customer> GetUserFromAccessToken(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                SecurityToken securityToken;
                var principle = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);

                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    var customerId = principle.FindFirst(ClaimTypes.Name)?.Value;

                    return await _context.Customers
                                        .Where(u => u.CustomerId == Convert.ToInt32(customerId)).FirstOrDefaultAsync();
                }
            }
            catch (Exception)
            {
                return new Customer();
            }

            return new Customer();
        }
      
        // Create token for SignIn
        private RefreshToken GenerateRefreshToken()
        {
            RefreshToken refreshToken = new RefreshToken();

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
            }
            refreshToken.ExpiryDate = DateTime.UtcNow.AddMonths(6);

            return refreshToken;
        }
        //Create token for SignIn
        private string GenerateAccessToken(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // lấy secretkey để tạo ra phần signature cho accesstoken
            var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, Convert.ToString(userId))
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        // PUT: api/Customers/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }
            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Customers
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.

        //[HttpPost("SignUp")]
        //public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        //{
        //    _context.Customers.Add(customer);
        //    await _context.SaveChangesAsync();
        //    return CreatedAtAction("GetCustomer", new { id = customer.CustomerId }, customer);
        //}

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Customer>> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        private bool CustomerExists(int id)
        {
        

            return _context.Customers.Any(e => e.CustomerId == id);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<ApiResult<string>>> Authenticate([FromForm] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = _context.Customers.Where(cus => cus.Email == request.Email).FirstOrDefault();
            if (customer == null) return new ApiErrorResult<string>("Tài khoản không tồn tại");

            if(customer.Password != request.Password)
                return new ApiErrorResult<string>("Mật khẩu không chính xác");

            customer = await _context.Customers.
                Where(cus => cus.Email == customer.Email && cus.Password == customer.Password)
                .FirstOrDefaultAsync();

            // tạo đối tượng CustomerWithToken
            CustomerWithToken customerWithToken = new CustomerWithToken(customer);

            // nếu có customer trong db
            if (customer != null)
            {
                // tạo ra refreshToken 
                RefreshToken refreshToken = GenerateRefreshToken();
                // add refreshtoken cho customer 
                customer.RefreshTokens.Add(refreshToken);
                // save việc insert đối tượng vào trong csdl 
                await _context.SaveChangesAsync();

                // tạo 1 đối tượng customerwithToken truyền vào là customer đã có refreshtoken
                customerWithToken = new CustomerWithToken(customer);
                // đối tượng customerWithToken đc đc refreshtoken
                customerWithToken.RefreshToken = refreshToken.Token;
            }

            if (customerWithToken == null)
            {
                return new ApiErrorResult<string>("Không có Token");
            }


            //    //sign your token here here..
            // tạo access token cho customer
            customerWithToken.AccessToken = GenerateAccessToken(customer.CustomerId);

            string access = customerWithToken.AccessToken;

            string refresh = customerWithToken.RefreshToken;

            return new ApiErrorResult<string>("Đăng nhập thành công")
            {
                IsSuccessed = true,
                customerWithToken= customerWithToken,
                
            };



        }

        [HttpPost("Register")]
        public async Task<ActionResult<ApiResult<bool>>> Register([FromForm] RegisterRequest request)
        {
            // thực hiện validate trên viewmodel trước, nếu validate ko thành công
            if (!ModelState.IsValid)
                return BadRequest(ModelState); 
           
            if( _context.Customers.Where(cus => cus.CustomerName == request.CustomerName).FirstOrDefault() !=  null)
            {
                return new ApiErrorResult<bool>("Tài khoản đã tồn tại");
            }
            // email đã đc 1 customer đăng kí rồi
            if (_context.Customers.Where(cus => cus.Email == request.Email).FirstOrDefault() != null)
                return new ApiErrorResult<bool>("Emai đã tồn tại");

           
                var customer = new Customer()
                {
                    CustomerName = request.CustomerName,
                    Address = request.Address,
                    City = request.City,
                    Phone = request.Phone,
                    District = request.District,
                    Password = request.Password,
                    Email = request.Email
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                return new ApiSuccessResult<bool>() { Message = "Đăng kí thành công"};
            
            // khi validate đã success thì tiến hành đăng kí với đối tượng truyền vào là ViewModel RegisterRequest
            // Lấy thông tin của ViewModel truyền xuống cho Model rồi lưu vào DB
            //var result = await _userService.Register(request);
            //if (!result)
            //{
            //    return BadRequest("Register is unsuccessful.");
            //}
            //return Ok();
        }
    }
}
