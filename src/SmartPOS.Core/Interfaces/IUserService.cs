using SmartPOS.Core.Entities;

namespace SmartPOS.Core.Interfaces
{
    public interface IUserService
    {
        User? CurrentUser { get; }
        void SetUser(User user);
        bool IsAdmin { get; }
        void Logout();
    }
}
