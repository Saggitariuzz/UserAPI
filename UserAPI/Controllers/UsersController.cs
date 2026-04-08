using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserAPI.DTOs;
using UserAPI.Services;
using UserAPI.Mappers;
using MongoDB.Bson;

namespace UserAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
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
    }
}
