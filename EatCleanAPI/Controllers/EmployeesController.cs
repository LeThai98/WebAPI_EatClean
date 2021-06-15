using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EatCleanAPI.Catalog.Common;
using EatCleanAPI.Models;
using EatCleanAPI.ViewModels;
using EatCleanAPI.ViewModels.Model_Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EatCleanAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : Controller
    {
        private readonly VegafoodContext _context;
        private readonly JWTSettings _jwtsettings;

        public EmployeesController(VegafoodContext context, IOptions<JWTSettings> jwtsettings)
        {
            _context = context;
            _jwtsettings = jwtsettings.Value;
        }

        [HttpGet]
        public async Task<List<Employee>> GetEmployees()
        {
            return await _context.Employees.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployeesById(int id)
        {
            var employee = await _context.Employees.Where(em => em.EmployeeId == id).FirstAsync();
            if (employee == null)
                return NotFound();
            return employee;
        }

        [HttpPost("SignUp")]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Employee>> DeleteEmployees(int id)
        {
            var employee = _context.Employees.Where(em => em.EmployeeId == id).First();
            if (employee == null)
                return NotFound();
            else
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                return NoContent();
            }    
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Employee>> PutEmployee(int id, Employee employee)
        {
            if (id != employee.EmployeeId)
                return BadRequest();
            _context.Entry(employee).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(employee => employee.EmployeeId == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpPost("SignIn")]
        public async Task<ActionResult<Employee>> login([FromBody] Employee employee)
        {
            // tìm ra customer trong db
            employee = await _context.Employees.
                Where(em => em.Email == employee.Email && em.Password == em.Password)
                .FirstOrDefaultAsync();

            // tạo đối tượng CustomerWithToken
            EmployeeWithToken employeerWithToken = new EmployeeWithToken(employee);

            // nếu có customer trong db
            if (employee != null)
            {
                // tạo ra refreshToken
                RefreshTokenEmployee refreshToken = GenerateRefreshToken();
                // add refreshtoken cho customer
                employee.RefreshTokenEmployees.Add(refreshToken);

                await _context.SaveChangesAsync();

                // tạo 1 đối tượng customerwithToken truyền vào là customer
                employeerWithToken = new EmployeeWithToken(employee);

                employeerWithToken.RefreshToken = refreshToken.Token;
            }

            if (employeerWithToken == null)
            {
                return NotFound();
            }
            employeerWithToken.AccessToken = GenerateAccessToken(employee.EmployeeId);
            return employeerWithToken;

        }

        [HttpPost("RefreshToken")]
        public async Task<ActionResult<Employee>> RefreshToken([FromBody] RefreshRequest refreshRequest)
        {
            Employee employee = await GetEmployeesFromAccessToken(refreshRequest.AccessToken);

            if (employee != null && ValidateRefreshToken(employee, refreshRequest.RefreshToken))
            {
                EmployeeWithToken customerWithToken = new EmployeeWithToken(employee);
                customerWithToken.AccessToken = GenerateAccessToken(employee.EmployeeId);

                return customerWithToken;
            }

            return null;
        }
        private RefreshTokenEmployee GenerateRefreshToken()
        {
            RefreshTokenEmployee refreshToken = new RefreshTokenEmployee();

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
            }
            refreshToken.ExpiryDate = DateTime.UtcNow.AddMonths(6);

            return refreshToken;
        }
        private string GenerateAccessToken(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
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
        private async Task<Employee> GetEmployeesFromAccessToken(string accessToken)
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
                    var employeeId = principle.FindFirst(ClaimTypes.Name)?.Value;

                    return await _context.Employees
                                        .Where(u => u.EmployeeId == Convert.ToInt32(employeeId)).FirstOrDefaultAsync();
                }
            }
            catch (Exception)
            {
                return new Employee();
            }

            return new Employee();
        }
        private bool ValidateRefreshToken(Employee employee, string refreshToken)
        {

            RefreshTokenEmployee refreshTokenEmployee = _context.RefreshTokenEmployees.Where(rt => rt.Token == refreshToken)
                                                .OrderByDescending(rt => rt.ExpiryDate)
                                                .FirstOrDefault();

            if (refreshTokenEmployee != null && refreshTokenEmployee.EmployeeId == employee.EmployeeId
                && refreshTokenEmployee.ExpiryDate > DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }

        [HttpPost("Login")]
        public async Task<ActionResult<ApiResult<string>>> Authenticate([FromForm] LoginRequestEmployee request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = _context.Employees.Where(em => em.Email == request.Email).FirstOrDefault();
            if (employee == null) return new ApiErrorResult<string>("Tài khoản không tồn tại");

            if (employee.Password != request.Password)
                return new ApiErrorResult<string>("Mật khẩu không chính xác");

            employee = await _context.Employees.
                Where(em => em.Email == employee.Email && em.Password == employee.Password)
                .FirstOrDefaultAsync();

            // tạo đối tượng CustomerWithToken
            EmployeeWithToken employeeWithToken = new EmployeeWithToken(employee);

            // nếu có customer trong db
            if (employee != null)
            {
                // tạo ra refreshToken 
                RefreshTokenEmployee refreshToken = GenerateRefreshToken();
                // add refreshtoken cho customer 
                employee.RefreshTokenEmployees.Add(refreshToken);
                // save việc insert đối tượng vào trong csdl 
                await _context.SaveChangesAsync();

                // tạo 1 đối tượng customerwithToken truyền vào là customer đã có refreshtoken
                employeeWithToken = new EmployeeWithToken(employee);
                // đối tượng customerWithToken đc đc refreshtoken
                employeeWithToken.RefreshToken = refreshToken.Token;
            }

            if (employeeWithToken == null)
            {
                return new ApiErrorResult<string>("Không có Token");
            }


            //    //sign your token here here..
            // tạo access token cho customer
            employeeWithToken.AccessToken = GenerateAccessToken(employee.EmployeeId);

            string access = employeeWithToken.AccessToken;

            string refresh = employeeWithToken.RefreshToken;

            return new ApiErrorResult<string>("Đăng nhập thành công")
            {
                IsSuccessed = true,
                employeeWithToken = employeeWithToken,

            };



        }

        [HttpPost("Register")]
        public async Task<ActionResult<ApiResult<bool>>> Register([FromForm] RegisterRequestEmployee request)
        {
            // thực hiện validate trên viewmodel trước, nếu validate ko thành công
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

           
            // email đã đc 1 customer đăng kí rồi
            if (_context.Employees.Where(em => em.Email == request.Email).FirstOrDefault() != null)
                return new ApiErrorResult<bool>("Emai đã tồn tại");


            var employee = new Employee()
            {
               FirstName = request.FirstName,
               LastName =request.LastName,
               Gender = request.Gender,
               BirthDate = request.Birthdate,
               HireDate = request.HireDate,
               Address = request.Address,
               City = request.City,
               District= request.District,
               Phone= request.Phone,
               Notes= request.Notes,
               Email= request.Email,
               Password= request.Password,
               Photo= request.Photo,

            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return new ApiSuccessResult<bool>() { Message = "Tạo mới nhân viên thành công" };

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
