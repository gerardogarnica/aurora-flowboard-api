namespace Aurora.Flowboard.Application.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(User.MaxNameLength);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(User.MaxNameLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength)
            .EmailAddress();

        RuleFor(x => x.Password)
            .MustBeStrongPassword();

        RuleFor(x => x.Role)
            .NotEmpty();
    }
}
