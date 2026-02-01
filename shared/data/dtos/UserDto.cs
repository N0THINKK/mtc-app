namespace mtc_app.shared.data.dtos
{
    public class UserDto
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Nik { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
