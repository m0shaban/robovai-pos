using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.Infrastructure.Services
{
    public class CurrentUserService : IUserService
    {
        public User? CurrentUser { get; private set; }

        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin || CurrentUser?.Role == UserRole.Manager;

        public void SetUser(User user)
        {
            CurrentUser = user;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
