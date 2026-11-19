using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.Abstractions.Validations;
using Aurora.Flowboard.Application.Authentication.Logout;
using Aurora.Flowboard.Application.Users.CreateUser;
using Aurora.Flowboard.Application.Users.GetUserById;
using FluentValidation;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors;

public sealed class ValidationBehaviorTests
{
    private const string ErrorCode = "Users.EmailInvalid";
    private const string ErrorMessage = "The email is not valid.";
    private static readonly Guid UserId = new("6f1c2d3e-4b5a-4c7d-8e9f-0a1b2c3d4e5f");

    [Fact]
    public async Task Should_PassCancellationTokenToValidators_When_CommandWithResponseIsHandled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        CreateUserCommand command = CreateUserCommand();

        ICommandHandler<CreateUserCommand, Guid> innerHandler = Substitute.For<ICommandHandler<CreateUserCommand, Guid>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok(UserId));

        IValidator<CreateUserCommand> validator = ValidValidator<CreateUserCommand>();
        var behavior = new ValidationBehavior.CommandHandler<CreateUserCommand, Guid>(innerHandler, [validator]);

        // Act
        await behavior.Handle(command, cancellationToken);

        // Assert
        await validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken);
    }

    [Fact]
    public async Task Should_PassCancellationTokenToValidators_When_CommandWithoutResponseIsHandled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        var command = new LogoutCommand("refresh-token");

        ICommandHandler<LogoutCommand> innerHandler = Substitute.For<ICommandHandler<LogoutCommand>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        IValidator<LogoutCommand> validator = ValidValidator<LogoutCommand>();
        var behavior = new ValidationBehavior.CommandBaseHandler<LogoutCommand>(innerHandler, [validator]);

        // Act
        await behavior.Handle(command, cancellationToken);

        // Assert
        await validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken);
    }

    [Fact]
    public async Task Should_PassCancellationTokenToValidators_When_QueryIsHandled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        var query = new GetUserByIdQuery(UserId);

        IQueryHandler<GetUserByIdQuery, UserProfileResponse> innerHandler =
            Substitute.For<IQueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        innerHandler.Handle(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserProfileResponse>(UserErrors.NotFound));

        IValidator<GetUserByIdQuery> validator = ValidValidator<GetUserByIdQuery>();
        var behavior = new ValidationBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>(innerHandler, [validator]);

        // Act
        await behavior.Handle(query, cancellationToken);

        // Assert
        await validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken);
    }

    [Fact]
    public async Task Should_ReturnValidationErrorWithoutCallingHandler_When_ValidationFails()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommand();

        ICommandHandler<CreateUserCommand, Guid> innerHandler = Substitute.For<ICommandHandler<CreateUserCommand, Guid>>();

        IValidator<CreateUserCommand> validator = Substitute.For<IValidator<CreateUserCommand>>();
        validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Email", ErrorMessage) { ErrorCode = ErrorCode }]));

        var behavior = new ValidationBehavior.CommandHandler<CreateUserCommand, Guid>(innerHandler, [validator]);

        // Act
        Result<Guid> result = await behavior.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        ValidationError validationError = result.Error.Should().BeOfType<ValidationError>().Subject;
        validationError.Errors.Should().ContainSingle(e => e.Code == ErrorCode && e.Message == ErrorMessage);
        await innerHandler.DidNotReceive().Handle(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CallInnerHandler_When_NoValidatorsAreRegistered()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommand();

        ICommandHandler<CreateUserCommand, Guid> innerHandler = Substitute.For<ICommandHandler<CreateUserCommand, Guid>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok(UserId));

        var behavior = new ValidationBehavior.CommandHandler<CreateUserCommand, Guid>(innerHandler, []);

        // Act
        Result<Guid> result = await behavior.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(UserId);
    }

    private static CreateUserCommand CreateUserCommand() =>
        new("Ada", "Lovelace", "ada@flowboard.dev", "Sup3r-Secret-Pa$$word", "Member");

    private static IValidator<T> ValidValidator<T>()
    {
        IValidator<T> validator = Substitute.For<IValidator<T>>();
        validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        return validator;
    }
}
