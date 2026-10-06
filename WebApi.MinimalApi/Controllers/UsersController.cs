using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
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
        
        if (user.Login?.Any(letter => !char.IsLetterOrDigit(letter)) == true)
        {
            ModelState.AddModelError("Login", "Login contains invalid characters");
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
    
    [HttpPut("{userId}")]
    public IActionResult UpdateUser(
        [FromBody] UserToUpdateDto? user,
        [FromRoute] string userId)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        if (!Guid.TryParse(userId, out var id))
        {
            return BadRequest();
        }
        
        var userEntityToUpdate = mapper.Map(
            user, 
            userRepository.FindById(id) ?? new UserEntity(id));

        userRepository.UpdateOrInsert(userEntityToUpdate, out var isInserted);
        
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                mapper.Map<UserDto>(userEntityToUpdate));
        }
        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDoc,
        [FromRoute] string userId)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }
        
        if (!Guid.TryParse(userId, out var id))
        {
            return NotFound();
        }

        var userEntity = userRepository.FindById(id);

        if (userEntity is null)
        {
            return NotFound();
        }

        var userDto = mapper.Map<UserToUpdateDto>(userEntity);

        patchDoc.ApplyTo(userDto, ModelState);

        if (!TryValidateModel(userDto))
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(userDto, userEntity);

        userRepository.UpdateOrInsert(userEntity, out _);

        return NoContent();
    }
}