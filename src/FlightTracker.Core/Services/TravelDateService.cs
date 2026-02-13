using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Core.Services;

/// <summary>
/// Service for managing travel dates with associated business logic.
/// Encapsulates operations that span multiple repositories.
/// </summary>
public class TravelDateService
{
    private readonly ITargetDateRepository _targetDateRepository;
    private readonly IPriceCheckRepository _priceCheckRepository;
    private readonly ILogger<TravelDateService> _logger;

    public TravelDateService(
        ITargetDateRepository targetDateRepository,
        IPriceCheckRepository priceCheckRepository,
        ILogger<TravelDateService> logger)
    {
        _targetDateRepository = targetDateRepository;
        _priceCheckRepository = priceCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new travel date with destination associations.
    /// </summary>
    public async Task<TravelDateResult> CreateAsync(
        string name,
        DateTime outboundDate,
        DateTime returnDate,
        IEnumerable<int> destinationIds,
        CancellationToken cancellationToken = default)
    {
        var destIds = destinationIds.ToList();
        
        // Validation
        if (string.IsNullOrWhiteSpace(name))
            return TravelDateResult.Failure("Name is required");
        
        if (returnDate <= outboundDate)
            return TravelDateResult.Failure("Return date must be after outbound date");
        
        if (!destIds.Any())
            return TravelDateResult.Failure("At least one destination is required");

        try
        {
            var targetDate = new TargetDate
            {
                Name = name.Trim(),
                OutboundDate = outboundDate,
                ReturnDate = returnDate
            };

            var created = await _targetDateRepository.CreateTargetDateAsync(targetDate, cancellationToken);
            
            if (created.Id <= 0)
            {
                _logger.LogError("Created target date has invalid ID: {Id}", created.Id);
                return TravelDateResult.Failure("Failed to create travel date");
            }

            await _targetDateRepository.UpdateDestinationsAsync(created.Id, destIds, cancellationToken);
            
            _logger.LogInformation(
                "Created travel date '{Name}' (ID: {Id}) with {Count} destinations",
                created.Name, created.Id, destIds.Count);

            return TravelDateResult.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating travel date '{Name}'", name);
            return TravelDateResult.Failure($"Error creating travel date: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a travel date and its destinations.
    /// Invalidates price history for removed destinations.
    /// </summary>
    public async Task<TravelDateResult> UpdateAsync(
        int targetDateId,
        string name,
        DateTime outboundDate,
        DateTime returnDate,
        IEnumerable<int> destinationIds,
        CancellationToken cancellationToken = default)
    {
        var newDestIds = destinationIds.ToHashSet();
        
        // Validation
        if (string.IsNullOrWhiteSpace(name))
            return TravelDateResult.Failure("Name is required");
        
        if (returnDate <= outboundDate)
            return TravelDateResult.Failure("Return date must be after outbound date");
        
        if (!newDestIds.Any())
            return TravelDateResult.Failure("At least one destination is required");

        try
        {
            // Get current destinations
            var currentDestinations = await _targetDateRepository.GetDestinationsAsync(targetDateId, cancellationToken);
            var currentDestIds = currentDestinations.Select(d => d.Id).ToHashSet();

            // Find removed destinations
            var removedDestIds = currentDestIds.Except(newDestIds).ToList();

            // Invalidate prices for removed destinations
            if (removedDestIds.Any())
            {
                var deletedCount = await _priceCheckRepository.DeleteOrphanedPriceChecksAsync(
                    targetDateId, 
                    newDestIds, 
                    cancellationToken);
                
                _logger.LogInformation(
                    "Invalidated {Count} price checks for removed destinations on travel date {Id}",
                    deletedCount, targetDateId);
            }

            // Update the target date
            var targetDate = new TargetDate
            {
                Id = targetDateId,
                Name = name.Trim(),
                OutboundDate = outboundDate,
                ReturnDate = returnDate
            };

            var updated = await _targetDateRepository.UpdateTargetDateAsync(targetDate, cancellationToken);
            if (!updated)
            {
                _logger.LogWarning("Failed to update target date {Id} - may have been deleted", targetDateId);
                return TravelDateResult.Failure("Travel date not found or has been deleted");
            }

            // Update destination associations
            await _targetDateRepository.UpdateDestinationsAsync(targetDateId, newDestIds, cancellationToken);

            _logger.LogInformation(
                "Updated travel date '{Name}' (ID: {Id}) with {Count} destinations",
                name, targetDateId, newDestIds.Count);

            return TravelDateResult.Success(targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating travel date {Id}", targetDateId);
            return TravelDateResult.Failure($"Error updating travel date: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft delete a travel date.
    /// </summary>
    public async Task<TravelDateResult> DeleteAsync(int targetDateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _targetDateRepository.SoftDeleteAsync(targetDateId, cancellationToken);
            if (!deleted)
            {
                return TravelDateResult.Failure("Travel date not found or already deleted");
            }

            _logger.LogInformation("Soft deleted travel date {Id}", targetDateId);
            return TravelDateResult.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting travel date {Id}", targetDateId);
            return TravelDateResult.Failure($"Error deleting travel date: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore a soft-deleted travel date.
    /// </summary>
    public async Task<TravelDateResult> RestoreAsync(int targetDateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var restored = await _targetDateRepository.RestoreAsync(targetDateId, cancellationToken);
            if (!restored)
            {
                return TravelDateResult.Failure("Travel date not found or not deleted");
            }

            _logger.LogInformation("Restored travel date {Id}", targetDateId);
            return TravelDateResult.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring travel date {Id}", targetDateId);
            return TravelDateResult.Failure($"Error restoring travel date: {ex.Message}");
        }
    }
}

/// <summary>
/// Result of a travel date operation.
/// </summary>
public class TravelDateResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public TargetDate? TravelDate { get; private set; }

    private TravelDateResult() { }

    public static TravelDateResult Success(TargetDate? travelDate) => new()
    {
        IsSuccess = true,
        TravelDate = travelDate
    };

    public static TravelDateResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
