using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ChangePassword;
using SmartTaxi.Application.Identity.Commands.ConfirmEmailVerification;
using SmartTaxi.Application.Identity.Commands.ConfirmPhoneVerification;
using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.DisableTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.ForgotPassword;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.Logout;
using SmartTaxi.Application.Identity.Commands.RefreshToken;
using SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Referrals.Commands.RegisterReferral;
using SmartTaxi.Application.Identity.Commands.RequestEmailVerification;
using SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;
using SmartTaxi.Application.Identity.Commands.ResetPassword;
using SmartTaxi.Application.Identity.Commands.RevokeAllSessions;
using TwoFactorChallengeCommandNs = SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

namespace SmartTaxi.API.Endpoints.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("LoginUser")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/revoke-all-sessions", RevokeAllSessionsAsync)
            .WithName("RevokeAllSessions")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/email-verification/request", RequestEmailVerificationAsync)
            .WithName("RequestEmailVerification")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/email-verification/confirm", ConfirmEmailVerificationAsync)
            .WithName("ConfirmEmailVerification")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/phone-verification/request", RequestPhoneVerificationAsync)
            .WithName("RequestPhoneVerification")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/phone-verification/confirm", ConfirmPhoneVerificationAsync)
            .WithName("ConfirmPhoneVerification")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/password/forgot", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/password/reset", ResetPasswordAsync)
            .WithName("ResetPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/password/change", ChangePasswordAsync)
            .WithName("ChangePassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/enroll", EnrollTwoFactorAsync)
            .WithName("EnrollTwoFactor")
            .RequireAuthorization()
            .Produces<EnrollTwoFactorResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/confirm", ConfirmTwoFactorAsync)
            .WithName("ConfirmTwoFactor")
            .RequireAuthorization()
            .Produces<RecoveryCodesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/2fa/challenge", TwoFactorChallengeAsync)
            .WithName("TwoFactorChallenge")
            .Produces<TwoFactorChallengeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/disable", DisableTwoFactorAsync)
            .WithName("DisableTwoFactor")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/recovery-code", RegenerateRecoveryCodesAsync)
            .WithName("RegenerateRecoveryCodes")
            .RequireAuthorization()
            .Produces<RecoveryCodesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Results<Created<RegisterUserResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> RegisterAsync(
        RegisterUserRequest request,
        RegisterUserCommandHandler handler,
        RegisterReferralCommandHandler referralHandler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Password);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var isConflict = result.ErrorType == ErrorType.Conflict;

            var problem = new ProblemDetails
            {
                Title = isConflict ? "Conflit" : "Requête invalide",
                Detail = result.Error,
                Status = isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest
            };

            return isConflict
                ? TypedResults.Conflict(problem)
                : TypedResults.BadRequest(problem);
        }

        // Best-effort: an invalid/unknown referral code never fails
        // registration — the account is still created either way.
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            await referralHandler.Handle(
                new RegisterReferralCommand(request.ReferralCode.Trim(), result.Value!.UserId), cancellationToken);
        }

        var response = new RegisterUserResponse(result.Value!.UserId, result.Value.Email);
        return TypedResults.Created($"/api/users/{response.UserId}", response);
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        LoginUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password, request.DeviceLabel);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                title: "Non autorisé",
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new LoginResponse(
            result.Value!.RequiresTwoFactor,
            result.Value.AccessToken,
            result.Value.RefreshToken,
            result.Value.TwoFactorChallengeToken));
    }

    private static async Task<Results<Ok<RefreshTokenResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshTokenRequest request,
        RefreshTokenCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

        if (!result.IsSuccess)
        {
            var isConflict = result.ErrorType == ErrorType.Conflict;

            return TypedResults.Problem(
                title: isConflict ? "Conflit" : "Non autorisé",
                detail: result.Error,
                statusCode: isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new RefreshTokenResponse(result.Value!.AccessToken, result.Value.RefreshToken));
    }

    private static async Task<NoContent> LogoutAsync(
        ClaimsPrincipal currentUser,
        LogoutCommandHandler handler,
        CancellationToken cancellationToken)
    {
        // The session to revoke always comes from the caller's own token (the
        // "sid" claim) — never from anything the client could supply directly.
        var sessionId = Guid.Parse(currentUser.FindFirstValue("sid")!);
        await handler.Handle(new LogoutCommand(sessionId), cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RevokeAllSessionsAsync(
        ClaimsPrincipal currentUser,
        RevokeAllSessionsCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await handler.Handle(new RevokeAllSessionsCommand(userId), cancellationToken);

        return TypedResults.NoContent();
    }

    // Always 204, regardless of outcome — must never reveal whether the email
    // belongs to an account, is already verified, or was rate-limited.
    private static async Task<NoContent> RequestEmailVerificationAsync(
        RequestEmailVerificationRequest request,
        RequestEmailVerificationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new RequestEmailVerificationCommand(request.Email), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>>> ConfirmEmailVerificationAsync(
        ConfirmEmailVerificationRequest request,
        ConfirmEmailVerificationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ConfirmEmailVerificationCommand(request.Token), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>>> RequestPhoneVerificationAsync(
        RequestPhoneVerificationRequest request,
        ClaimsPrincipal currentUser,
        RequestPhoneVerificationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RequestPhoneVerificationCommand(userId, request.PhoneNumber), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>>> ConfirmPhoneVerificationAsync(
        ConfirmPhoneVerificationRequest request,
        ClaimsPrincipal currentUser,
        ConfirmPhoneVerificationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ConfirmPhoneVerificationCommand(userId, request.Otp), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    // Always 204, regardless of outcome — must never reveal whether the email
    // belongs to an account.
    private static async Task<NoContent> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        ForgotPasswordCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new ForgotPasswordCommand(request.Email), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>>> ResetPasswordAsync(
        ResetPasswordRequest request,
        ResetPasswordCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ProblemHttpResult>> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal currentUser,
        ChangePasswordCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var sessionId = Guid.Parse(currentUser.FindFirstValue("sid")!);

        var command = new ChangePasswordCommand(userId, sessionId, request.CurrentPassword, request.NewPassword);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.Unauthorized)
            {
                return TypedResults.Problem(title: "Non autorisé", detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
            }

            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<EnrollTwoFactorResponse>, ProblemHttpResult>> EnrollTwoFactorAsync(
        ClaimsPrincipal currentUser,
        EnrollTwoFactorCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new EnrollTwoFactorCommand(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(title: "Non autorisé", detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new EnrollTwoFactorResponse(result.Value!.Secret, result.Value.AuthenticatorUri));
    }

    private static async Task<Results<Ok<RecoveryCodesResponse>, BadRequest<ProblemDetails>>> ConfirmTwoFactorAsync(
        ConfirmTwoFactorRequest request,
        ClaimsPrincipal currentUser,
        ConfirmTwoFactorCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ConfirmTwoFactorCommand(userId, request.Code), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.Ok(new RecoveryCodesResponse(result.Value!.RecoveryCodes));
    }

    private static async Task<Results<Ok<TwoFactorChallengeResponse>, ProblemHttpResult>> TwoFactorChallengeAsync(
        TwoFactorChallengeRequest request,
        TwoFactorChallengeCommandNs.TwoFactorChallengeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(request.ChallengeToken, request.Code), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(title: "Non autorisé", detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new TwoFactorChallengeResponse(result.Value!.AccessToken, result.Value.RefreshToken));
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ProblemHttpResult>> DisableTwoFactorAsync(
        DisableTwoFactorRequest request,
        ClaimsPrincipal currentUser,
        DisableTwoFactorCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new DisableTwoFactorCommand(userId, request.CurrentPassword, request.Code);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.Unauthorized)
            {
                return TypedResults.Problem(title: "Non autorisé", detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
            }

            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<RecoveryCodesResponse>, BadRequest<ProblemDetails>>> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal currentUser,
        RegenerateRecoveryCodesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RegenerateRecoveryCodesCommand(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.Ok(new RecoveryCodesResponse(result.Value!.RecoveryCodes));
    }
}
