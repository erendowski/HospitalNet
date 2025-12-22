namespace HospitalNet.UI.Security
{
    public sealed class UserSession
    {
        public UserSession(string username, bool isAdmin)
        {
            Username = username;
            IsAdmin = isAdmin;
        }

        public string Username { get; }
        public bool IsAdmin { get; }
    }
}
