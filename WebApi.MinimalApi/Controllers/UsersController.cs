using System.Linq;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using Newtonsoft.Json;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly LinkGenerator _linkGenerator;
    
    
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        if (HttpMethods.IsHead(Request.Method))
            return Content(string.Empty, "application/json; charset=utf-8");
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
        
        var userEntity = new UserEntity()
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
            GamesPlayed = 0,
        };
        var insertedUserEntity = _userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUserEntity.Id },
            insertedUserEntity.Id);
    }

    private static UserDto GetUserDtoByUserEntity(UserEntity user)
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

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateToDto>? patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }
        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }

        var userUpdate = new UserUpdateToDto(userEntity.Login, userEntity.FirstName, userEntity.LastName); 
        patchDoc.ApplyTo(userUpdate, ModelState);

        if (!ModelState.IsValid || !TryValidateModel(userUpdate))
        {
            return UnprocessableEntity(ModelState);
        }
        _userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }
        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 20);
        pageNumber = Math.Max(pageNumber, 1);
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = pageList.Select(x => GetUserDtoByUserEntity(x));
        var paginationHeader = new
        {
            previousPageLink = pageNumber == 1 ? null : _linkGenerator.GetUriByRouteValues(
                HttpContext, 
                nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize }),
            nextPageLink = _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize }),
            totalCount = pageList.TotalCount,
            pageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }

    [HttpOptions(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
    
}