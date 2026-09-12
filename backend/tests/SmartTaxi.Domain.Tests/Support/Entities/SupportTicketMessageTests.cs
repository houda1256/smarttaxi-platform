using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Domain.Tests.Support.Entities;

public class SupportTicketMessageTests
{
    [Fact]
    public void Create_WithValidFields_SetsAllProperties()
    {
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var message = SupportTicketMessage.Create(ticketId, authorId, "Bonjour, pouvez-vous préciser le problème ?", false, now);

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(ticketId, message.TicketId);
        Assert.Equal(authorId, message.AuthorUserId);
        Assert.False(message.IsInternalNote);
        Assert.Equal(now, message.CreatedAtUtc);
    }

    [Fact]
    public void Create_AsInternalNote_SetsFlag()
    {
        var message = SupportTicketMessage.Create(Guid.NewGuid(), Guid.NewGuid(), "Note interne pour l'équipe", true, DateTime.UtcNow);

        Assert.True(message.IsInternalNote);
    }

    [Fact]
    public void Create_WithEmptyTicketId_Throws()
    {
        Assert.Throws<ArgumentException>(() => SupportTicketMessage.Create(Guid.Empty, Guid.NewGuid(), "Body", false, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingBody_Throws(string body)
    {
        Assert.Throws<ArgumentException>(() => SupportTicketMessage.Create(Guid.NewGuid(), Guid.NewGuid(), body, false, DateTime.UtcNow));
    }
}
