namespace CW_7_s30320.Models.DTOs;

public class ClientTripDTO
{
    public int    ClientId     { get; set; }
    public int    TripId       { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
}