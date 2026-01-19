using System.Threading.Tasks;
using mtc_app.features.rating.data.dtos;

namespace mtc_app.features.rating.data.repositories
{
    public interface IRatingRepository
    {
        /// <summary>
        /// Submits a general rating (e.g. user satisfaction).
        /// </summary>
        Task SubmitRatingAsync(RatingDto rating);
    }
}
