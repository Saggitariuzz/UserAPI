using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Необходимо указать логин!")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Необходимо указать пароль!")]
        public string Password { get; set; } = string.Empty;
    }
}
