using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserCreateToDto
{
    [Required]
    public string Login { get; set; }
    [DefaultValue("John")]
    public string FirstName { get; set; }
    [DefaultValue("Doe")]
    public string LastName { get; set; }
    public UserCreateToDto(string login, string firstName, string lastName)
    {
        Login = login;
        FirstName = firstName;
        LastName = lastName;
    }
}