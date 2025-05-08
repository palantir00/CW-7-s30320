using CW_7_s30320.Models.DTOs;

namespace CW_7_s30320.Services;

public interface ITripsService
{

    Task<List<TripDTO>> GetTrips();

    Task<List<TripDTO>> GetTripsForClient(int clientId);
    
    Task<int> CreateClient(ClientDTO client);
    
    Task<bool> RegisterClientToTrip(int clientId, int tripId);
    
    Task<bool> UnregisterClientFromTrip(int clientId, int tripId);
}