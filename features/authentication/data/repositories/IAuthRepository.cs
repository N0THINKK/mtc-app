using System.Threading.Tasks;
using mtc_app.shared.data.dtos;

namespace mtc_app.features.authentication.data.repositories
{
    public interface IAuthRepository
    {
        Task<UserDto> LoginAsync(string username, string password);
    }
}
