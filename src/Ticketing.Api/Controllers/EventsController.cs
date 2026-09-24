using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Events;
using Ticketing.Application.Events.Dtos;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create(CreateEventRequest request,CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateAsync(request,cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventResponse>> GetById(Guid id,CancellationToken cancellationToken)
    {
        var result = await _eventService.GetByIdAsync(id,cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _eventService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventResponse>> Update(Guid id,UpdateEventRequest request,CancellationToken cancellationToken)
    {
        var result = await _eventService.UpdateAsync(id,request,cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id,CancellationToken cancellationToken)
    {
        var deleted = await _eventService.DeleteAsync(id,cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}