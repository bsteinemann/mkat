using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class ServiceContactTests
{
    [Fact]
    public void ServiceContact_HasCompositeKey()
    {
        var serviceId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        var sc = new ServiceContact
        {
            ServiceId = serviceId,
            ContactId = contactId
        };

        Assert.Equal(serviceId, sc.ServiceId);
        Assert.Equal(contactId, sc.ContactId);
    }

    [Fact]
    public void ServiceContact_ServiceNavigation_IsNull()
    {
        var sc = new ServiceContact();
        Assert.Null(sc.Service);
    }

    [Fact]
    public void ServiceContact_ContactNavigation_IsNull()
    {
        var sc = new ServiceContact();
        Assert.Null(sc.Contact);
    }
}
