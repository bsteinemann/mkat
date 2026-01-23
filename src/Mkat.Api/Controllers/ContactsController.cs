using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactRepository _contactRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ContactsController(IContactRepository contactRepo, IUnitOfWork unitOfWork)
    {
        _contactRepo = contactRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateContactRequest request,
        [FromServices] IValidator<CreateContactRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        await _contactRepo.AddAsync(contact, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Created($"/api/v1/contacts/{contact.Id}", MapToResponse(contact));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var contacts = await _contactRepo.GetAllAsync(ct);
        var responses = contacts.Select(MapToResponse).ToList();
        return Ok(responses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var contact = await _contactRepo.GetByIdWithChannelsAsync(id, ct);
        if (contact == null)
            return NotFound();

        return Ok(MapToResponse(contact));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateContactRequest request,
        [FromServices] IValidator<UpdateContactRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var contact = await _contactRepo.GetByIdWithChannelsAsync(id, ct);
        if (contact == null)
            return NotFound();

        contact.Name = request.Name;
        await _contactRepo.UpdateAsync(contact, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(MapToResponse(contact));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var contact = await _contactRepo.GetByIdAsync(id, ct);
        if (contact == null)
            return NotFound();

        if (contact.IsDefault)
            return BadRequest(new { error = "Cannot delete the default contact." });

        if (await _contactRepo.IsOnlyContactForAnyServiceAsync(id, ct))
            return BadRequest(new { error = "Cannot delete contact that is the only contact for a service." });

        await _contactRepo.DeleteAsync(contact, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    private static ContactResponse MapToResponse(Contact contact)
    {
        return new ContactResponse
        {
            Id = contact.Id,
            Name = contact.Name,
            IsDefault = contact.IsDefault,
            CreatedAt = contact.CreatedAt,
            Channels = contact.Channels.Select(ch => new ContactChannelResponse
            {
                Id = ch.Id,
                Type = ch.Type,
                Configuration = ch.Configuration,
                IsEnabled = ch.IsEnabled,
                CreatedAt = ch.CreatedAt
            }).ToList(),
            ServiceCount = contact.ServiceContacts.Count
        };
    }
}
