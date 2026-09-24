using Ticketing.Application.Events.Dtos;

namespace Ticketing.Application.Events;

public interface IEventService
{
    Task<EventResponse> CreateAsync(CreateEventRequest request,CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<EventResponse?> GetByIdAsync(Guid id,CancellationToken cancellationToken = default);

    Task<EventResponse?> UpdateAsync(Guid id,UpdateEventRequest request,CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id,CancellationToken cancellationToken = default);
}