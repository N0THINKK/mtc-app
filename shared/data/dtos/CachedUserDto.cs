namespace mtc_app.shared.data.dtos
{
    /// <summary>
    /// DTO for cached user data from remote database.
    /// </summary>
    public class CachedUserDto
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Nik { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
