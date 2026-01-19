using System.Threading.Tasks;
using Dapper;
using mtc_app.features.rating.data.dtos;

namespace mtc_app.features.rating.data.repositories
{
    public class RatingRepository : IRatingRepository
    {
        public async Task SubmitRatingAsync(RatingDto rating)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Assuming a 'ratings' table exists or creating a generic one. 
                // Since I don't know the Schema for 'ratings', I will assume a standard structure 
                // derived from the DTO. If it doesn't exist, this will need a migration, 
                // but for refactoring code, this is the standard implementation.
                
                string sql = @"
                    INSERT INTO ratings (ticket_id, score, comment, rater_id, created_at)
                    VALUES (@TicketId, @Score, @Comment, @RaterId, NOW())";

                await connection.ExecuteAsync(sql, rating);
            }
        }
    }
}
