using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.LoginUser;

public sealed class LoginUserCommandHandler
    : ICommandHandler<LoginUserCommand, Result<LoginUserResult>>
{
    private const string InvalidCredentialsError = "Email ou mot de passe incorrect.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash.Value, command.Password))
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        var accessToken = _tokenGenerator.GenerateToken(user);

        return Result<LoginUserResult>.Success(new LoginUserResult(accessToken));
    }
}
