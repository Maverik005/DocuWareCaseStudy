using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using EventRegistration.Core.ExceptionHandlers;
using EventRegistration.Core.Interfaces;
using EventRegistration.Core.Mappings;
using EventRegistration.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EventRegistration.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RegistrationsController : ControllerBase
{
    private readonly IUnitOfWork _UnitOfWork;
    private readonly ILogger<RegistrationsController> _logger;
    private readonly EventLimitsConfiguration _eventLimits;

    public RegistrationsController(
       IUnitOfWork unitOfWork,
       ILogger<RegistrationsController> logger, IOptions<EventLimitsConfiguration> eventLimits)
    {
        _UnitOfWork = unitOfWork;
        _logger = logger;
        _eventLimits = eventLimits.Value;
    }

    //Includes duplicate registration check
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> RegisterForEvent(
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Verify event exists and is not in the past
            var eventEntity = await _UnitOfWork.EventRepository.GetEventByIdAsync(
                request.EventId,
                false,
                cancellationToken);

            if (eventEntity == null)
                return BadRequest(new { message = "Event not found" });

            if (eventEntity.StartTime < DateTime.UtcNow)
                return BadRequest(new { message = "Cannot register for past events" });

            // Check registration limit
            var currentRegistrationCount = await _UnitOfWork.EventRepository.GetRegistrationCountAsync(
                request.EventId,
                cancellationToken);

            var (limitValid, limitError) = EventValidationUtility.ValidateRegistrationLimit(
                currentRegistrationCount,
                _eventLimits.MaxRegistrationsPerEvent);

            if (!limitValid)
            {
                _logger.LogWarning(
                    "Event {EventId} has reached registration limit of {Limit}",
                    request.EventId, _eventLimits.MaxRegistrationsPerEvent);
                return BadRequest(new { message = limitError });
            }

            // Check for duplicate registration
            var existingRegistration = await _UnitOfWork.RegistrationRepository
                .IsEmailRegisteredAsync(
                    request.EventId,
                    request.EmailAddress,
                    cancellationToken);

            if (existingRegistration == true)
            {
                _logger.LogWarning(
                    "Duplicate registration attempted: Email={Email}, EventId={EventId}",
                    request.EmailAddress, request.EventId);
                return Conflict(new
                {
                    message = $"{request.EmailAddress} is already registered for this event",
                });
            }

            // Create registration
            var registration = new Registration
            {
                EventId = request.EventId,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                EmailAddress = request.EmailAddress,
                RegistrationSource = request.RegistrationSource ?? "Web"
            };

            var createdRegistration = await _UnitOfWork.RegistrationRepository
                .CreateRegistrationAsync(registration, cancellationToken);

            var response = EntityMapper.MapToRegistrationResponse(createdRegistration);

            return CreatedAtAction(
                nameof(GetRegistration),
                new { id = createdRegistration.Id },
                response);
        }
        catch (DuplicateRegistrationException ex)
        {
            _logger.LogWarning(
                "Duplicate registration attempt: EventId={EventId}, Email={Email}",
                ex.EventId,
                ex.Email);

            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating registration for event {EventId}", request.EventId);
            return StatusCode(500, "An error occurred while processing your registration");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> GetRegistration(
        int id,
        CancellationToken cancellationToken)
    {
        var registration = await _UnitOfWork.RegistrationRepository.GetRegistrationByIdAsync(id, cancellationToken);

        if (registration == null)
            return NotFound(new { message = $"Registration with ID {id} not found" });

        return Ok(EntityMapper.MapToRegistrationResponse(registration));
    }
}
