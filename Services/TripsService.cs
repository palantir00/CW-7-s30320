using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;         
using CW_7_s30320.Models.DTOs;

namespace CW_7_s30320.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
        public async Task<List<TripDTO>> GetTrips()
        {
            var trips = new List<TripDTO>();
            const string sql = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                       c.Name AS CountryName
                FROM Trip t
                LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                LEFT JOIN Country c      ON ct.IdCountry = c.IdCountry
                ORDER BY t.IdTrip;
            ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd  = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                int id   = rdr.GetInt32(0);
                var trip = trips.FirstOrDefault(x => x.Id == id);
                if (trip == null)
                {
                    trip = new TripDTO {
                        Id          = id,
                        Name        = rdr.GetString(1),
                        Description = rdr.GetString(2),
                        DateFrom    = rdr.GetDateTime(3),
                        DateTo      = rdr.GetDateTime(4),
                        MaxPeople   = rdr.GetInt32(5),
                        Countries   = new List<CountryDTO>()
                    };
                    trips.Add(trip);
                }

                if (!rdr.IsDBNull(6))
                    trip.Countries.Add(new CountryDTO {
                        Name = rdr.GetString(6)
                    });
            }
            return trips;
        }
        
        public async Task<List<TripDTO>> GetTripsForClient(int clientId)
        {
            var list = new List<TripDTO>();
            const string chk = "SELECT COUNT(*) FROM Client WHERE IdClient = @cid";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using (var c = new SqlCommand(chk, conn))
            {
                c.Parameters.AddWithValue("@cid", clientId);
                if ((int)await c.ExecuteScalarAsync() == 0)
                    return null;
            }
            
            const string sql = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate, c.Name AS CountryName
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                LEFT JOIN Country_Trip crt ON t.IdTrip = crt.IdTrip
                LEFT JOIN Country c       ON crt.IdCountry = c.IdCountry
                WHERE ct.IdClient = @cid
                ORDER BY t.IdTrip;
            ";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", clientId);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                int id = rdr.GetInt32(0);
                var trip = list.FirstOrDefault(x => x.Id == id);
                if (trip == null)
                {
                    trip = new TripDTO {
                        Id          = id,
                        Name        = rdr.GetString(1),
                        Description = rdr.GetString(2),
                        DateFrom    = rdr.GetDateTime(3),
                        DateTo      = rdr.GetDateTime(4),
                        MaxPeople   = rdr.GetInt32(5),
                        Countries   = new List<CountryDTO>(),
                        Registration = new ClientTripDTO {
                            ClientId     = clientId,
                            TripId       = id,
                            RegisteredAt = rdr.GetDateTime(6),
                            PaymentDate  = rdr.IsDBNull(7) ? null : rdr.GetDateTime(7)
                        }
                    };
                    list.Add(trip);
                }
                if (!rdr.IsDBNull(8))
                    trip.Countries.Add(new CountryDTO { Name = rdr.GetString(8) });
            }
            return list;
        }

        public async Task<int> CreateClient(ClientDTO client)
        {
            const string sql = @"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@fn,@ln,@em,@tel,@pes);
            ";
            using var conn = new SqlConnection(_connectionString);
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@fn", client.FirstName);
            cmd.Parameters.AddWithValue("@ln", client.LastName);
            cmd.Parameters.AddWithValue("@em", client.Email);
            cmd.Parameters.AddWithValue("@tel", client.Telephone);
            cmd.Parameters.AddWithValue("@pes", client.Pesel);
            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync();
        }
        
        public async Task<bool> RegisterClientToTrip(int clientId, int tripId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            using (var c = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient=@cid", conn))
            {
                c.Parameters.AddWithValue("@cid", clientId);
                if ((int)await c.ExecuteScalarAsync() == 0) return false;
            }

            using (var c = new SqlCommand("SELECT COUNT(*) FROM Trip WHERE IdTrip=@tid", conn))
            {
                c.Parameters.AddWithValue("@tid", tripId);
                if ((int)await c.ExecuteScalarAsync() == 0) return false;
            }

            int regCount, max;
            using (var c = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip=@tid", conn))
            {
                c.Parameters.AddWithValue("@tid", tripId);
                regCount = (int)await c.ExecuteScalarAsync();
            }
            using (var c = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip=@tid", conn))
            {
                c.Parameters.AddWithValue("@tid", tripId);
                max = (int)await c.ExecuteScalarAsync();
            }
            if (regCount >= max) return false;
            
            using (var c = new SqlCommand(
                "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@cid,@tid,GETDATE())",
                conn))
            {
                c.Parameters.AddWithValue("@cid", clientId);
                c.Parameters.AddWithValue("@tid", tripId);
                await c.ExecuteNonQueryAsync();
            }
            return true;
        }

        public async Task<bool> UnregisterClientFromTrip(int clientId, int tripId)
        {
            const string sql = 
                "DELETE FROM Client_Trip WHERE IdClient=@cid AND IdTrip=@tid";
            using var conn = new SqlConnection(_connectionString);
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", clientId);
            cmd.Parameters.AddWithValue("@tid", tripId);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
}