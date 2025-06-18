using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace A3;
public class GamificationService
{
    private readonly string _connectionString;

    public GamificationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AddPointsAsync(long petId, int points)
    {
        using var con = new MySqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new MySqlCommand(@"INSERT INTO pet_points (pet_id, points) VALUES (@petId, @points) ON DUPLICATE KEY UPDATE points = points + @points", con);
        cmd.Parameters.AddWithValue("@petId", petId);
        cmd.Parameters.AddWithValue("@points", points);

        await cmd.ExecuteNonQueryAsync();

        await CheckAndAddBadgesAsync(petId, con);
    }

    private async Task CheckAndAddBadgesAsync(long petId, MySqlConnection con)
    {
        var pointsCmd = new MySqlCommand("SELECT points FROM pet_points WHERE pet_id = @petId", con);
        pointsCmd.Parameters.AddWithValue("@petId", petId);
        var pointsObj = await pointsCmd.ExecuteScalarAsync();
        int points = pointsObj != null ? Convert.ToInt32(pointsObj) : 0;

        if (points >= 100)
        {
            var badgeCmd = new MySqlCommand("INSERT IGNORE INTO pet_badges (pet_id, badge_name) VALUES (@petId, 'Super Pet')", con);
            badgeCmd.Parameters.AddWithValue("@petId", petId);
            await badgeCmd.ExecuteNonQueryAsync();
        }

        if (points >= 200)
        {
            var badgeCmd = new MySqlCommand("INSERT IGNORE INTO pet_badges (pet_id, badge_name) VALUES (@petId, 'Veteran Pet')", con);
            badgeCmd.Parameters.AddWithValue("@petId", petId);
            await badgeCmd.ExecuteNonQueryAsync();
        }

        if (points + points >= 300)
        {
            var badgeCmd = new MySqlCommand("INSERT IGNORE INTO pet_badges (pet_id, badge_name) VALUES (@petId, 'Ultimate Pet')", con);
            badgeCmd.Parameters.AddWithValue("@petId", petId);
            await badgeCmd.ExecuteNonQueryAsync();
        }
    }


    public async Task<int> GetPointsAsync(long petId)
    {
        using var con = new MySqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new MySqlCommand("SELECT points FROM pet_points WHERE pet_id = @petId", con);
        cmd.Parameters.AddWithValue("@petId", petId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<List<string>> GetBadgesAsync(long petId)
    {
        using var con = new MySqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new MySqlCommand("SELECT badge_name FROM pet_badges WHERE pet_id = @petId", con);
        cmd.Parameters.AddWithValue("@petId", petId);

        var badges = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            badges.Add(reader.GetString("badge_name"));
        }
        return badges;
    }
}
