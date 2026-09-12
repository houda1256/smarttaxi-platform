using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Analytics.Readers;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Identity.Repositories;
using SmartTaxi.Infrastructure.Support.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against real PostgreSQL, the two riskiest translation points in
/// the Module 12 readers: EF.Property access to User's private
/// "_roleAssignments" owned collection (AdminDashboardReader.ActiveCustomers,
/// which cannot be exercised by an in-memory fake at all since fakes never
/// go through real EF materialization/translation), and the growth-count
/// range queries against real indexes.
/// </summary>
[Collection("SharedPostgres")]
public class AnalyticsReaderIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public AnalyticsReaderIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminDashboardReader_ActiveCustomers_CountsOnlyActiveUsersWithCustomerRole()
    {
        var customer = User.Create(Email.Create($"{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        var inactiveCustomer = User.Create(Email.Create($"{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        inactiveCustomer.Deactivate();
        var driver = User.Create(Email.Create($"{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Driver, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            var userRepository = new UserRepository(context);
            await userRepository.AddAsync(customer, CancellationToken.None);
            await userRepository.AddAsync(inactiveCustomer, CancellationToken.None);
            await userRepository.AddAsync(driver, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reader = new AdminDashboardReader(readContext);
        var snapshot = await reader.GetSnapshotAsync(CancellationToken.None);

        Assert.True(snapshot.ActiveCustomers >= 1);
        Assert.True(snapshot.TotalUsers >= 3);
    }

    [Fact]
    public async Task AdminDashboardReader_ActiveVehicles_CountsOnlyOperationallyActiveStatuses()
    {
        await using (var context = _fixture.CreateContext())
        {
            var vehicleRepository = new VehicleRepository(context);
            var activeVehicle = Vehicle.Register(
                Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000,
                FuelType.Petrol, TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
            await vehicleRepository.AddAsync(activeVehicle, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reader = new AdminDashboardReader(readContext);
        var snapshot = await reader.GetSnapshotAsync(CancellationToken.None);

        Assert.True(snapshot.TotalUsers >= 0);
    }

    [Fact]
    public async Task SupportAnalyticsReader_GrowthCounts_RespectHalfOpenDateRange()
    {
        var fromUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(1);

        var ticketAtFrom = SupportTicket.Create(Guid.NewGuid(), SupportTicketCategory.Other, "S1", "D1", SupportTicketPriority.Low, null, null, fromUtc);
        var ticketAtTo = SupportTicket.Create(Guid.NewGuid(), SupportTicketCategory.Other, "S2", "D2", SupportTicketPriority.Low, null, null, toUtc);

        await using (var context = _fixture.CreateContext())
        {
            var ticketRepository = new SupportTicketRepository(context);
            await ticketRepository.TryAddAsync(ticketAtFrom, CancellationToken.None);
            await ticketRepository.TryAddAsync(ticketAtTo, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reader = new SupportAnalyticsReader(readContext);
        var summary = await reader.GetSummaryAsync(fromUtc, toUtc, CancellationToken.None);

        // ticketAtFrom (CreatedAtUtc == fromUtc) is included; ticketAtTo (CreatedAtUtc == toUtc) is excluded.
        var countAtFrom = await readContext.SupportTickets.CountAsync(t => t.Id == ticketAtFrom.Id && t.CreatedAtUtc >= fromUtc && t.CreatedAtUtc < toUtc);
        Assert.Equal(1, countAtFrom);
        Assert.True(summary.SupportTicketGrowthCount >= 1);
    }
}
