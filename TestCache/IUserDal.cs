using System;
namespace TestCache
{
    public interface IUserDal
    {
        void AddUser(User u);
        void EditUser(User u);
        void DeleteUserById(int id);
        System.Collections.Generic.List<User> GetAllUsers();
        User GetUserById(int id);
    }
}
