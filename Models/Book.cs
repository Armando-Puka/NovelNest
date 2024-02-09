#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
namespace NovelNest.Models;

public class Book {
    [Key]
    public int BookId { get; set; }

    [Required]
    public byte[] BookCover { get; set; }
    
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

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public int? UserId { get; set; }
    public User? BookAuthor { get; set; }

}