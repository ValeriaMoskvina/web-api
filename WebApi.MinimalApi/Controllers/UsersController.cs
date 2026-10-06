using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserToCreateDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }
        
        if (string.IsNullOrEmpty(user?.Login) || user.Login.Any(letter => !char.IsLetterOrDigit(letter)))
        {
            ModelState.AddModelError("Login", $"Login contains invalid characters");
        }
        
        if (ModelState.IsValid == false)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            mapper.Map<UserDto>(createdUserEntity));
    }
}