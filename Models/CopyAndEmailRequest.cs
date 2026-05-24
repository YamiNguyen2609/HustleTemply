using System.ComponentModel.DataAnnotations;

namespace HustleTemply.Models;

public class CopyAndEmailRequest
{
    [Required]
    public string SheetId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
