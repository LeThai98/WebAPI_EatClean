using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.Models
{
    public class EmployeeWithToken : Employee
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public EmployeeWithToken(Employee employee)
        {
            this.EmployeeId = employee.EmployeeId;
            this.FirstName = employee.FirstName;
            this.LastName = employee.LastName;
            this.HireDate = employee.HireDate;
            this.Phone = employee.Phone;
            this.Photo = employee.Photo;
            this.Address = employee.Address;
            this.BirthDate = employee.BirthDate;
            this.City = employee.City;
            this.District = employee.District;
            this.Email = employee.Email;
            this.Gender = employee.Gender;
            this.Notes = employee.Notes;
            this.Password = employee.Password;
        }
    }
}
