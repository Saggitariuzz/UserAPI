using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserAPI.DTOs;
using UserAPI.Services;
using UserAPI.Mappers;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using UserAPI.Settings;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Prometheus;

namespace UserAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        private readonly ITokenService _tokenService;

        private readonly ITokenBlackListService _tokenBlackListService;

        private static readonly Counter LoginCounter = Metrics.CreateCounter("auth_login_total", "Количество попыток входа", "status");
        
        private static readonly Counter LogoutCounter = Metrics.CreateCounter("auth_logout_total", "Количество выходов из системы");

        public UsersController(IUserService userService, ITokenService tokenService, ITokenBlackListService tokenBlackListService)
        {
            _userService = userService;
            _tokenService = tokenService;
            _tokenBlackListService = tokenBlackListService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserReadDto>>> GetAll()
        {
            var entites = await _userService.GetAllAsync();
            var dtos = entites.Select(UserMapper.MapUserToUserReadDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<UserReadDto>> GetById(string id)
        {
            if(!ObjectId.TryParse(id, out _))
            {
                return BadRequest(new { message = "Некорректный формат ID" });
            }
            var entity = await _userService.GetUserAsync(id);
            if(entity is null)
            {
                return NotFound(new { message = $"Пользователь с ID {id} не найден" });
            }
            var dto = UserMapper.MapUserToUserReadDto(entity);
            return Ok(dto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserReadDto>> Create([FromBody] UserCreateDto ucd)
        {
            var entity = UserMapper.MapUserCreateDtoToUser(ucd);
            await _userService.CreateAsync(entity);
            var dto = UserMapper.MapUserToUserReadDto(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto uud)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest(new { message = "Некорректный формат ID" });
            }
            var user = await _userService.GetUserAsync(id);
            if(user is null)
            {
                return NotFound();
            }
            UserMapper.MapUserUpdateDtoToUser(uud, user);
            await _userService.UpdateAsync(id, user);
            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest(new { message = "Некорректный формат ID" });
            }
            var user = await _userService.GetUserAsync(id);
            if (user is null)
            {
                return NotFound();
            }
            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto uld)
        {
            var user = await _userService.AuthenticateAsync(uld.Login, uld.Password);
            if(user == null)
            {
                LoginCounter.WithLabels("failure").Inc();
                return Unauthorized(new {message = "Неверный логин или пароль"});
            }
            LoginCounter.WithLabels("success").Inc();
            var encodedToken = _tokenService.GenerateToken(user);
            return Ok(new { token = encodedToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = await HttpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }
            LogoutCounter.Inc();
            await _tokenBlackListService.DeactivateTokenAsync(token);
            return Ok(new { message = "Вы успешно вышли из учетной записи" });
        }
    }
}
