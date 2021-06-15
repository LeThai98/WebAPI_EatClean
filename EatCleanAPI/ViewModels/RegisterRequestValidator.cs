using FluentValidation;

namespace EatCleanAPI.ViewModels
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Customer is required")
                .MaximumLength(30).WithMessage("CustomerName can not over 30 characters");

            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required")
                .MaximumLength(60).WithMessage("Address can not over 60 characters");

            RuleFor(x => x.City).NotEmpty().WithMessage("City is required")
                .MaximumLength(15).WithMessage("Address can not over 15 characters");

            RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required")
                .MaximumLength(10).WithMessage("Phone can not over 10 characters");

            RuleFor(x => x.District).NotEmpty().WithMessage("District is required")
                .MaximumLength(15).WithMessage("District can not over 15 characters");

            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
                .Matches(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$")
                .WithMessage("Email format not match");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password is at least 6 characters")
                .MaximumLength(15).WithMessage("Password can not over 15 characters");

            RuleFor(x => x).Custom((request, context) =>
            {
                if (request.Password != request.ConfirmPassword)
                {
                    context.AddFailure("Confirm password is not match");
                }
            });
        }
    }
}