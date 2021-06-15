﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.ViewModels.Model_Employee
{
    public class RegisterRequestEmployee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Gender { get; set; }
        public DateTime Birthdate { get; set; }
        public DateTime HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string District { get; set; }
        public string Notes { get; set; }
        public string Photo { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
       // public string ConfirmPassword { get; set; }
    }
}
