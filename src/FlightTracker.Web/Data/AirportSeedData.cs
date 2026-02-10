using FlightTracker.Core.Entities;

namespace FlightTracker.Web.Data;

/// <summary>
/// Comprehensive list of European and popular international airports.
/// </summary>
public static class AirportSeedData
{
    /// <summary>
    /// Get all airports to seed into the database.
    /// Focused on popular destinations from Germany.
    /// </summary>
    public static List<Destination> GetAirports()
    {
        return new List<Destination>
        {
            // Spain - Popular beach destinations
            new Destination { AirportCode = "PMI", Name = "Palma de Mallorca" },
            new Destination { AirportCode = "AGP", Name = "Málaga" },
            new Destination { AirportCode = "ALC", Name = "Alicante" },
            new Destination { AirportCode = "BCN", Name = "Barcelona" },
            new Destination { AirportCode = "MAD", Name = "Madrid" },
            new Destination { AirportCode = "SVQ", Name = "Sevilla" },
            new Destination { AirportCode = "VLC", Name = "Valencia" },
            new Destination { AirportCode = "IBZ", Name = "Ibiza" },
            
            // Canary Islands
            new Destination { AirportCode = "TFS", Name = "Tenerife South" },
            new Destination { AirportCode = "TFN", Name = "Tenerife North" },
            new Destination { AirportCode = "LPA", Name = "Gran Canaria" },
            new Destination { AirportCode = "FUE", Name = "Fuerteventura" },
            new Destination { AirportCode = "ACE", Name = "Lanzarote" },
            
            // Italy
            new Destination { AirportCode = "FCO", Name = "Rome Fiumicino" },
            new Destination { AirportCode = "CIA", Name = "Rome Ciampino" },
            new Destination { AirportCode = "MXP", Name = "Milan Malpensa" },
            new Destination { AirportCode = "LIN", Name = "Milan Linate" },
            new Destination { AirportCode = "BGY", Name = "Milan Bergamo" },
            new Destination { AirportCode = "VCE", Name = "Venice Marco Polo" },
            new Destination { AirportCode = "NAP", Name = "Naples" },
            new Destination { AirportCode = "CTA", Name = "Catania" },
            new Destination { AirportCode = "PMO", Name = "Palermo" },
            new Destination { AirportCode = "BLQ", Name = "Bologna" },
            new Destination { AirportCode = "PSA", Name = "Pisa" },
            new Destination { AirportCode = "FLR", Name = "Florence" },
            
            // Greece
            new Destination { AirportCode = "ATH", Name = "Athens" },
            new Destination { AirportCode = "HER", Name = "Heraklion (Crete)" },
            new Destination { AirportCode = "RHO", Name = "Rhodes" },
            new Destination { AirportCode = "CFU", Name = "Corfu" },
            new Destination { AirportCode = "JMK", Name = "Mykonos" },
            new Destination { AirportCode = "JTR", Name = "Santorini" },
            new Destination { AirportCode = "SKG", Name = "Thessaloniki" },
            new Destination { AirportCode = "CHQ", Name = "Chania (Crete)" },
            new Destination { AirportCode = "ZTH", Name = "Zakynthos" },
            new Destination { AirportCode = "KGS", Name = "Kos" },
            
            // Portugal
            new Destination { AirportCode = "LIS", Name = "Lisbon" },
            new Destination { AirportCode = "OPO", Name = "Porto" },
            new Destination { AirportCode = "FAO", Name = "Faro (Algarve)" },
            new Destination { AirportCode = "FNC", Name = "Funchal (Madeira)" },
            new Destination { AirportCode = "PDL", Name = "Ponta Delgada (Azores)" },
            
            // France
            new Destination { AirportCode = "CDG", Name = "Paris Charles de Gaulle" },
            new Destination { AirportCode = "ORY", Name = "Paris Orly" },
            new Destination { AirportCode = "NCE", Name = "Nice" },
            new Destination { AirportCode = "MRS", Name = "Marseille" },
            new Destination { AirportCode = "LYS", Name = "Lyon" },
            new Destination { AirportCode = "TLS", Name = "Toulouse" },
            new Destination { AirportCode = "BOD", Name = "Bordeaux" },
            new Destination { AirportCode = "NTE", Name = "Nantes" },
            
            // UK & Ireland
            new Destination { AirportCode = "LHR", Name = "London Heathrow" },
            new Destination { AirportCode = "LGW", Name = "London Gatwick" },
            new Destination { AirportCode = "STN", Name = "London Stansted" },
            new Destination { AirportCode = "LCY", Name = "London City" },
            new Destination { AirportCode = "LTN", Name = "London Luton" },
            new Destination { AirportCode = "MAN", Name = "Manchester" },
            new Destination { AirportCode = "EDI", Name = "Edinburgh" },
            new Destination { AirportCode = "GLA", Name = "Glasgow" },
            new Destination { AirportCode = "BHX", Name = "Birmingham" },
            new Destination { AirportCode = "DUB", Name = "Dublin" },
            
            // Scandinavia
            new Destination { AirportCode = "ARN", Name = "Stockholm Arlanda" },
            new Destination { AirportCode = "NYO", Name = "Stockholm Skavsta" },
            new Destination { AirportCode = "CPH", Name = "Copenhagen" },
            new Destination { AirportCode = "OSL", Name = "Oslo" },
            new Destination { AirportCode = "HEL", Name = "Helsinki" },
            new Destination { AirportCode = "BGO", Name = "Bergen" },
            new Destination { AirportCode = "GOT", Name = "Gothenburg" },
            
            // Eastern Europe
            new Destination { AirportCode = "PRG", Name = "Prague" },
            new Destination { AirportCode = "BUD", Name = "Budapest" },
            new Destination { AirportCode = "WAW", Name = "Warsaw" },
            new Destination { AirportCode = "KRK", Name = "Krakow" },
            new Destination { AirportCode = "VIE", Name = "Vienna" },
            new Destination { AirportCode = "BTS", Name = "Bratislava" },
            new Destination { AirportCode = "OTP", Name = "Bucharest" },
            new Destination { AirportCode = "SOF", Name = "Sofia" },
            
            // Benelux
            new Destination { AirportCode = "AMS", Name = "Amsterdam" },
            new Destination { AirportCode = "BRU", Name = "Brussels" },
            new Destination { AirportCode = "LUX", Name = "Luxembourg" },
            
            // Switzerland & Austria
            new Destination { AirportCode = "ZRH", Name = "Zurich" },
            new Destination { AirportCode = "GVA", Name = "Geneva" },
            new Destination { AirportCode = "BSL", Name = "Basel" },
            new Destination { AirportCode = "INN", Name = "Innsbruck" },
            new Destination { AirportCode = "SZG", Name = "Salzburg" },
            
            // Germany (for reference/connections)
            new Destination { AirportCode = "BER", Name = "Berlin Brandenburg" },
            new Destination { AirportCode = "MUC", Name = "Munich" },
            new Destination { AirportCode = "FRA", Name = "Frankfurt" },
            new Destination { AirportCode = "HAM", Name = "Hamburg" },
            new Destination { AirportCode = "DUS", Name = "Düsseldorf" },
            new Destination { AirportCode = "CGN", Name = "Cologne/Bonn" },
            new Destination { AirportCode = "STR", Name = "Stuttgart" },
            new Destination { AirportCode = "HAJ", Name = "Hannover" },
            new Destination { AirportCode = "NUE", Name = "Nuremberg" },
            
            // Balkans
            new Destination { AirportCode = "DBV", Name = "Dubrovnik" },
            new Destination { AirportCode = "SPU", Name = "Split" },
            new Destination { AirportCode = "ZAG", Name = "Zagreb" },
            new Destination { AirportCode = "BEG", Name = "Belgrade" },
            new Destination { AirportCode = "LJU", Name = "Ljubljana" },
            
            // Turkey
            new Destination { AirportCode = "IST", Name = "Istanbul" },
            new Destination { AirportCode = "SAW", Name = "Istanbul Sabiha Gökçen" },
            new Destination { AirportCode = "AYT", Name = "Antalya" },
            new Destination { AirportCode = "DLM", Name = "Dalaman" },
            new Destination { AirportCode = "ADB", Name = "Izmir" },
            new Destination { AirportCode = "BJV", Name = "Bodrum" },
            
            // North Africa & Middle East
            new Destination { AirportCode = "SSH", Name = "Sharm el-Sheikh" },
            new Destination { AirportCode = "HRG", Name = "Hurghada" },
            new Destination { AirportCode = "CAI", Name = "Cairo" },
            new Destination { AirportCode = "TUN", Name = "Tunis" },
            new Destination { AirportCode = "DJE", Name = "Djerba" },
            new Destination { AirportCode = "CMN", Name = "Casablanca" },
            new Destination { AirportCode = "RAK", Name = "Marrakech" },
            new Destination { AirportCode = "AGA", Name = "Agadir" },
            new Destination { AirportCode = "DXB", Name = "Dubai" },
            new Destination { AirportCode = "AUH", Name = "Abu Dhabi" },
            
            // Long-haul (selection)
            new Destination { AirportCode = "JFK", Name = "New York JFK" },
            new Destination { AirportCode = "EWR", Name = "New York Newark" },
            new Destination { AirportCode = "MIA", Name = "Miami" },
            new Destination { AirportCode = "LAX", Name = "Los Angeles" },
            new Destination { AirportCode = "YYZ", Name = "Toronto" },
            new Destination { AirportCode = "BKK", Name = "Bangkok" },
            new Destination { AirportCode = "SIN", Name = "Singapore" },
            new Destination { AirportCode = "HKG", Name = "Hong Kong" },
        };
    }
}
