#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace NovelNest.Models;

public class BookViewModel
{
    [Required]
    public IFormFile BookCover { get; set; }

    [Required]
    public string BookTitle { get; set; }

    [Required]
    public string BookDescription { get; set; }

    [Required]
    public int BookPages { get; set; }

    [Required]
    public int BookChapters { get; set; }

    [Required]
    public string BookGenre { get; set; }

    public Book? BookId { get; set; }
}