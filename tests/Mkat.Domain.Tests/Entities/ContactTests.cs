using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class ContactTests
{
    [Fact]
    public void Contact_HasRequiredProperties()
    {
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = "On-call Team",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, contact.Id);
        Assert.Equal("On-call Team", contact.Name);
        Assert.False(contact.IsDefault);
    }

    [Fact]
    public void Contact_DefaultName_IsEmpty()
    {
        var contact = new Contact();
        Assert.Equal(string.Empty, contact.Name);
    }

    [Fact]
    public void Contact_DefaultIsDefault_IsFalse()
    {
        var contact = new Contact();
        Assert.False(contact.IsDefault);
    }

    [Fact]
    public void Contact_Channels_InitializedEmpty()
    {
        var contact = new Contact();
        Assert.NotNull(contact.Channels);
        Assert.Empty(contact.Channels);
    }

    [Fact]
    public void Contact_ServiceContacts_InitializedEmpty()
    {
        var contact = new Contact();
        Assert.NotNull(contact.ServiceContacts);
        Assert.Empty(contact.ServiceContacts);
    }
}
