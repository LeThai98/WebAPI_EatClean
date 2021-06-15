using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.ViewModels.Model_Employee
{
    public class LoginRequestEmployee
    {
        public string Email { get; set; }

        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
