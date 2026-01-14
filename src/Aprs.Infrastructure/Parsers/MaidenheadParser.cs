using System;
using Aprs.Domain.ValueObjects;

namespace Aprs.Infrastructure.Parsers;

public static class MaidenheadParser
{
    // Basic converting of Maidenhead Grid to Lat/Long
    // Format: "JO91" or "JO91AB"
    // Pairs: [Fields][Squares][Subsquares]
    // A-R, 0-9, a-x
    
    public static GeoCoordinate? Parse(string grid)
    {
        if (string.IsNullOrWhiteSpace(grid) || grid.Length < 2) return null;
        
        grid = grid.Trim().ToUpperInvariant();
        
        // Validation: Should start with 2 letters
        if (grid[0] < 'A' || grid[0] > 'R' || grid[1] < 'A' || grid[1] > 'R') return null;

        double lon = -180.0;
        double lat = -90.0;
        
        // Field (A-R): 20x10 degrees
        lon += (grid[0] - 'A') * 20;
        lat += (grid[1] - 'A') * 10;
        
        if (grid.Length >= 4)
        {
            // Square (0-9): 2x1 degrees
            if (!char.IsDigit(grid[2]) || !char.IsDigit(grid[3])) return null;
            lon += (grid[2] - '0') * 2;
            lat += (grid[3] - '0') * 1;
        }
        else
        {
            // Center of the field
            lon += 10;
            lat += 5;
            return new GeoCoordinate(lat, lon);
        }
        
        if (grid.Length >= 6)
        {
            // Subsquare (a-x / A-X): 5x2.5 minutes (1/12 x 1/24 degrees)
            // 2 degrees / 24 = 0.08333 deg width
            // 1 degree / 24 = 0.04166 deg height
            
            char c1 = grid[4];
            char c2 = grid[5];
            
            // Should be letters
            if (c1 < 'A' || c1 > 'X' || c2 < 'A' || c2 > 'X') return null; // Standard says 'x' is 24th letter
            
            lon += (c1 - 'A') * (2.0 / 24.0);
            lat += (c2 - 'A') * (1.0 / 24.0);
            
            // Center of subsquare
             lon += (1.0 / 24.0);  // Center is +0.5 of width? Width=2/24. Half=1/24. Correct.
             lat += (0.5 / 24.0); // Height=1/24. Half=0.5/24. Correct.
        }
        else
        {
            // Center of Square
            lon += 1;
            lat += 0.5;
        }
        
        return new GeoCoordinate(lat, lon);
    }
}
