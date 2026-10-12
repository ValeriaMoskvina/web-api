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
    [Produces("application/json", "application/xml")]
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
        
        var userEntity = new UserEntity(Guid.NewGuid())
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
            GamesPlayed = 0,
        };
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
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

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UserUpdateToDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        var userId =  GetUserId();
        if (userId is null)
        {
            return BadRequest();
        }
        var userEntity = new UserEntity(userId.Value)
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
            GamesPlayed = 0,
        };
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity);
        }
        else
        {
            return NoContent();
        }
    }
    
    private Guid? GetUserId()
    {
        var rawUserId = RouteData.Values["userId"]?.ToString();
        Guid userId;
        try
        {
            userId = new Guid(rawUserId);
        }
        catch
        {
            return null;
        }
        return userId;
    }
}