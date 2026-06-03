using System;
using System.Threading.Tasks;
using Dapper;
using mtc_app.features.authentication.data.repositories;
using mtc_app.shared.data.dtos;
using mtc_app.shared.data.session;
using mtc_app.shared.data.utils;

namespace mtc_app.features.authentication.presentation.controllers
{
    public class LoginController
    {
        private readonly ILoginView _view;
        private readonly IAuthRepository _authRepository;
        private readonly ISetupRepository _setupRepository;

        public LoginController(ILoginView view, IAuthRepository authRepository, ISetupRepository setupRepository)
        {
            _view = view;
            _authRepository = authRepository;
            _setupRepository = setupRepository;
        }

        public async Task HandleLoginAsync()
        {
            string roleOrUser = _view.SelectedRole;
            string identity = _view.Identity;
            string password = _view.Password;

            if (roleOrUser == "Operator" || roleOrUser == "Teknisi" || roleOrUser == "Stock")
            {
                await ProcessRoleLoginAsync(roleOrUser, identity);
            }
            else
            {
                await ProcessAdminLoginAsync(roleOrUser, password);
            }
        }

        private async Task ProcessRoleLoginAsync(string role, string identity)
        {
            if (role == "Operator" && string.IsNullOrEmpty(identity))
            {
                _view.ShowWarning("Harap isi NIK Operator.");
                return;
            }

            if (role == "Operator")
            {
                _view.SaveOperatorNikToHistory(identity);
            }

            string fetchedFullName = null;
            long fetchedUserId = 0;
            
            if (!string.IsNullOrEmpty(identity))
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        var user = await conn.QueryFirstOrDefaultAsync<UserDto>(
                            "SELECT user_id as UserId, full_name as FullName FROM users WHERE username = @Username OR nik = @Username LIMIT 1",
                            new { Username = identity }
                        );

                        if (user != null)
                        {
                            fetchedUserId = user.UserId;
                            fetchedFullName = user.FullName;
                        }
                        else if (role == "Operator")
                        {
                            string insertSql = "INSERT INTO users (full_name, nik, username, role_id, password) VALUES (@Nik, @Nik, @Nik, 1, '123456'); SELECT LAST_INSERT_ID();";
                            fetchedUserId = await conn.ExecuteScalarAsync<long>(insertSql, new { Nik = identity });
                            fetchedFullName = identity;
                        }
                    }
                }
                catch { } // Abaikan jika offline
            }

            var sessionUser = new UserDto
            {
                UserId = fetchedUserId,
                Username = string.IsNullOrEmpty(identity) ? role : identity,
                RoleName = role,
                FullName = fetchedFullName
            };

            UserSession.SetUser(sessionUser);
            _view.HideForm();
            _view.ProceedToDashboard(sessionUser);
        }

        private async Task ProcessAdminLoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                _view.ShowWarning("Harap isi Username.");
                return;
            }

            _view.SetBusyState(true);

            try
            {
                UserDto user = await _authRepository.LoginAsync(username, password);
                if (user != null)
                {
                    UserSession.SetUser(user);
                    _view.ShowSuccess($"Login Berhasil! Selamat datang, {user.Username} ({user.RoleName})");
                    _view.HideForm();
                    _view.ProceedToDashboard(user);
                }
                else
                {
                    _view.ShowError("Username atau Password salah!", "Login Gagal");
                }
            }
            catch (Exception ex)
            {
                _view.ShowError($"Terjadi kesalahan database:\n{ex.Message}");
            }
            finally
            {
                _view.SetBusyState(false);
            }
        }
    }
}
