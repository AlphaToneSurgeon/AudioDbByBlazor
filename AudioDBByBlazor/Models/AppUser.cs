using Microsoft.AspNetCore.Identity;

namespace AudioDBByBlazor.Models;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
