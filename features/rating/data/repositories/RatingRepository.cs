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
                // PERBAIKAN LOGIKA:
                // Alih-alih INSERT ke tabel 'ratings' (yang datanya tidak terbaca di dashboard),
                // kita UPDATE kolom 'tech_rating_score' di tabel 'tickets'.
                
                string sql = @"
                    UPDATE tickets 
                    SET tech_rating_score = @Score, 
                        tech_rating_note = @Comment
                    WHERE ticket_id = @TicketId";

                // Dapper akan otomatis mencocokkan parameter @Score, @Comment, @TicketId 
                // dari properti yang ada di object 'rating' (RatingDto)
                await connection.ExecuteAsync(sql, rating);
            }
        }
    }
}