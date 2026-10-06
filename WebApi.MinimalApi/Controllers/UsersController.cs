using AutoMapper;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    
    
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        var userDto = GetUserDtoByUserEntity(user);
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateToDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(c => char.IsLetterOrDigit(c)))
        {
            ModelState.AddModelError("login", "login должен содержать только буквы и цифры");
            return UnprocessableEntity(ModelState);
        }
        var userEntity = new UserEntity()
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
            GamesPlayed = 0,
        };
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity);
    }

    private UserDto GetUserDtoByUserEntity(UserEntity user)
    {
        return new UserDto()
        {
            Id = user.Id, Login = user.Login,
            FullName = $"{user.FirstName} {user.LastName}",
            GamesPlayed = user.GamesPlayed,
            CurrentGameId = user.CurrentGameId,
        };
    }
}