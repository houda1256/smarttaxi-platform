using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class VehicleDocumentAccessAuditRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public VehicleDocumentAccessAuditRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_PersistsOneAuditEntryPerContentAccess()
    {
        var documentId = Guid.NewGuid();
        var firstAccessor = Guid.NewGuid();
        var secondAccessor = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new VehicleDocumentAccessAuditRepository(writeContext);
            await repository.AddAsync(
                new VehicleDocumentAccessAuditEntry(
                    documentId, firstAccessor, VehicleDocumentAccessType.ContentDownload, DateTime.UtcNow, "10.0.0.1"),
                CancellationToken.None);
            await repository.AddAsync(
                new VehicleDocumentAccessAuditEntry(
                    documentId, secondAccessor, VehicleDocumentAccessType.MetadataRead, DateTime.UtcNow, "10.0.0.2"),
                CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var entries = await readContext.VehicleDocumentAccessAuditEntries
            .Where(entry => entry.DocumentId == documentId)
            .OrderBy(entry => entry.AccessedByUserId == firstAccessor ? 0 : 1)
            .ToListAsync(CancellationToken.None);

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.AccessedByUserId == firstAccessor && e.AccessType == VehicleDocumentAccessType.ContentDownload);
        Assert.Contains(entries, e => e.AccessedByUserId == secondAccessor && e.AccessType == VehicleDocumentAccessType.MetadataRead);
        Assert.All(entries, e => Assert.NotNull(e.IpAddress));
    }

    [Fact]
    public async Task TryReplaceAsync_OnADocument_LeavesOriginalAndAuditTrailIntactAcrossVersions()
    {
        var vehicleId = Guid.NewGuid();
        var original = VehicleDocument.Upload(
            vehicleId, VehicleDocumentType.Insurance, "storage-ref-1", "insurance-v1.pdf", "application/pdf", 100,
            "hash-1", null, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new VehicleDocumentRepository(writeContext).AddAsync(original, CancellationToken.None);
            await new VehicleDocumentAccessAuditRepository(writeContext).AddAsync(
                new VehicleDocumentAccessAuditEntry(
                    original.Id, Guid.NewGuid(), VehicleDocumentAccessType.ContentDownload, DateTime.UtcNow, null),
                CancellationToken.None);
        }

        var replacement = original.CreateReplacement("storage-ref-2", "insurance-v2.pdf", "application/pdf", 200, "hash-2", null, null, DateTime.UtcNow);

        await using (var replaceContext = _fixture.CreateContext())
        {
            var replaced = await new VehicleDocumentRepository(replaceContext)
                .TryReplaceAsync(original, replacement, DateTime.UtcNow, CancellationToken.None);
            Assert.True(replaced);
        }

        await using var readContext = _fixture.CreateContext();
        var auditCountForOriginal = await readContext.VehicleDocumentAccessAuditEntries.CountAsync(e => e.DocumentId == original.Id);
        Assert.Equal(1, auditCountForOriginal);

        var reloadedOriginal = await readContext.VehicleDocuments.FirstAsync(d => d.Id == original.Id);
        Assert.Equal(VehicleDocumentStatus.Replaced, reloadedOriginal.Status);
    }
}
