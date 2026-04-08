using UserAPI.DTOs;
using UserAPI.Models;

namespace UserAPI.Mappers
{
    public class UserMapper
    {
        public static User MapUserCreateDtoToUser(UserCreateDto ucd)
        {
            return new User
            {
                Login = ucd.Login,
                Password = ucd.Password,
                RegisteredObjects = 0
            };
        }

        public static UserReadDto MapUserToUserReadDto(User user)
        {
            return new UserReadDto
            {
                Id = user.Id!,
                Login = user.Login,
                RegisteredObjects = user.RegisteredObjects
            };
        }

        public static void MapUserUpdateDtoToUser(UserUpdateDto uud, User user)
        {
            user.Login = uud.Login;
            user.Password = uud.Password;
        }
    }
}
