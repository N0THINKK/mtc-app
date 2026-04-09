using mtc_app.shared.data.dtos;

namespace mtc_app.shared.data.session
{
    public static class UserSession
    {
        private static UserDto _currentUser;

        public static UserDto CurrentUser => _currentUser;

        public static bool IsLoggedIn => _currentUser != null;

        public static void SetUser(UserDto user)
        {
            _currentUser = user;
        }

        public static void Clear()
        {
            _currentUser = null;
        }
    }
}
