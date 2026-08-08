using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Taxes.Commands.CreateTaxRule;
using SmartTaxi.Application.Payments.Taxes.Commands.DeactivateTaxRule;
using SmartTaxi.Application.Payments.Taxes.Queries.GetAllTaxRules;
using SmartTaxi.Application.Payments.Taxes.Queries.GetApplicableTaxRule;

namespace SmartTaxi.API.Endpoints.Payments;

public static class TaxRuleAdminEndpoints
{
    public static IEndpointRouteBuilder MapTaxRuleAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/tax-rules").WithTags("Tax Catalog").RequireAuthorization(Permissions.FinanceTaxManage);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTaxRule").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetAllAsync)
            .WithName("GetAllTaxRules").Produces<IReadOnlyCollection<TaxRuleResponse>>(StatusCodes.Status200OK);

        group.MapGet("/applicable", GetApplicableAsync)
            .WithName("GetApplicableTaxRule").Produces<TaxRuleResponse?>(StatusCodes.Status200OK);

        group.MapPost("/{taxRuleId:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateTaxRule").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateAsync(
        CreateTaxRuleRequest request, CreateTaxRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateTaxRuleCommand(
                request.TaxName, request.TaxRate, request.Jurisdiction, request.ApplicableService, request.EffectiveFrom,
                request.EffectiveTo, request.ExemptionRules),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<TaxRuleResponse>>> GetAllAsync(
        int pageNumber, int pageSize, GetAllTaxRulesQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetAllTaxRulesQuery(pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<TaxRuleResponse> response = result.Items.Select(TaxRuleResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<TaxRuleResponse?>> GetApplicableAsync(
        string applicableService, DateOnly date, GetApplicableTaxRuleQueryHandler handler, CancellationToken cancellationToken)
    {
        var rule = await handler.Handle(new GetApplicableTaxRuleQuery(applicableService, date), cancellationToken);
        TaxRuleResponse? response = rule is null ? null : TaxRuleResponse.FromEntity(rule);
        return TypedResults.Ok<TaxRuleResponse?>(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateAsync(
        Guid taxRuleId, DeactivateTaxRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeactivateTaxRuleCommand(taxRuleId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
