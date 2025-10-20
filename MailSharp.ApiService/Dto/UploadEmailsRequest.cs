using System.ComponentModel.DataAnnotations;

namespace MailSharp.ApiService.Dto;

public class UploadEmailsRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public int TemplateId { get; set; }
}
