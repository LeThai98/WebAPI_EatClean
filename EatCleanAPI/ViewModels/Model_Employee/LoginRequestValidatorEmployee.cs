using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EatCleanAPI.ViewModels.Model_Employee
{
    public class LoginRequestValidatorEmployee : AbstractValidator<LoginRequestEmployee>
    {
        public LoginRequestValidatorEmployee()
        {
          
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password is at least 6 characters");
        }
    }
}
