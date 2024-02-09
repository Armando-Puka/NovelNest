using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NovelNest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace NovelNest.Controllers;

public class SessionCheckAttribute : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        int? userId = context.HttpContext.Session.GetInt32("UserId");
        if (userId == null) {
            context.Result = new RedirectToActionResult("Auth", "Home", null);
        }
    }
}

public class BaseController : Controller {
    private MyContext _context;

    public BaseController(MyContext context) {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context) {
        base.OnActionExecuting(context);

        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        ViewBag.Username = user != null ? $"{user.Name} {user.LastName}" : "Username";

        if (user != null && user.ProfilePicture != null) {
            var base64 = Convert.ToBase64String(user.ProfilePicture);
            var imgSrc = string.Format("data:image/jpg;base64,{0}", base64);
            ViewBag.ProfilePicture = imgSrc;
        } else {
            ViewBag.ProfilePicture = null;
        }

        base.OnActionExecuting(context);
    }
}

public class HomeController : BaseController {
    private readonly ILogger<HomeController> _logger;
    private MyContext _context;

    public HomeController(ILogger<HomeController> logger, MyContext context) : base(context) {
        _logger = logger;
        _context = context;
    }

    [SessionCheck]
    public IActionResult Index() {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        ViewBag.Username = $"{user.Name} {user.LastName}";

        ViewBag.Books = _context.Books.Include(b => b.BookAuthor).OrderByDescending(b => b.CreatedAt).ToList();
        return View();
    }

    [HttpGet("Auth")]
    public IActionResult Auth() {
        return View();
    }

    [HttpPost("Register")]
    public IActionResult Register(User userFromForm) {
        if (ModelState.IsValid) {
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            userFromForm.Password = Hasher.HashPassword(userFromForm, userFromForm.Password);
            _context.Add(userFromForm);
            _context.SaveChanges();

            return RedirectToAction("Auth");
        }

        return View("Auth");
    }

    [HttpPost("Login")]
    public IActionResult Login(LoginUser registeredUser) {
        if (ModelState.IsValid) {
            User userFromDb = _context.Users.FirstOrDefault(e => e.Email == registeredUser.LoginEmail);

            if (userFromDb == null) {
                ModelState.AddModelError("LoginEmail", "Invalid email address.");
                return View("Auth");
            }

            PasswordHasher<LoginUser> Hasher = new PasswordHasher<LoginUser>();
            
            var result = Hasher.VerifyHashedPassword(registeredUser, userFromDb.Password, registeredUser.LoginPassword);

            if (result == 0) {
                ModelState.AddModelError("LoginPassword", "Invalid password.");
                return View("Auth");
            }

            HttpContext.Session.SetInt32("UserId", userFromDb.UserId);
            return RedirectToAction("Index");
        }

        return View("Auth");
    }

    private List<string> genres = new List<string> {
        "Action and Adventure",
        "Alternate History",
        "Anthology",
        "Art",
        "Autobiography",
        "Biography",
        "Chick Lit",
        "Classics",
        "Comics",
        "Contemporary",
        "Cookbooks",
        "Crime",
        "Drama",
        "Dystopian",
        "Economics",
        "Education",
        "Encyclopedia",
        "Fantasy",
        "Health",
        "Historical Fiction",
        "History",
        "Horror",
        "Humor and Comedy",
        "Journal",
        "Magic Realism",
        "Manga",
        "Memoir",
        "Mystery",
        "Mythology",
        "Nature",
        "Non-fiction",
        "Paranormal",
        "Philosophy",
        "Poetry",
        "Politics",
        "Psychology",
        "Reference",
        "Religion",
        "Romance",
        "Science",
        "Science Fiction",
        "Self Help",
        "Short Stories",
        "Social Science",
        "Spirituality",
        "Sports and Leisure",
        "Suspense",
        "Technology",
        "Thriller",
        "Travel",
        "True Crime",
        "War",
        "Western",
        "Young Adult",
    };

    [SessionCheck]
    [HttpGet("PublishABook")]
    public IActionResult PublishABook() {
        ViewBag.Genres = genres;
        return View();
    }

    [SessionCheck]
    [HttpPost("PublishABook")]
    public IActionResult PublishABook(BookViewModel model, IFormFile bookCover) {
        if (ModelState.IsValid) {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (bookCover != null && bookCover.Length > 0) {
                using (var memoryStream = new MemoryStream()) {
                    bookCover.CopyTo(memoryStream);

                    model.BookCover = new FormFile(memoryStream, 0, memoryStream.Length, "bookCover", "bookCover.jpg");

                    memoryStream.Position = 0;

                    byte[] bookCoverBytes = memoryStream.ToArray();

                    Book newBook = new Book {
                        BookCover = bookCoverBytes,
                        BookTitle = model.BookTitle,
                        BookDescription = model.BookDescription,
                        BookPages = model.BookPages,
                        BookChapters = model.BookChapters,
                        BookGenre = model.BookGenre,
                        UserId = (int)userId
                    };

                    _context.Add(newBook);
                    _context.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
        }

        ViewBag.Genres = genres;
        return View(model);
    }

    [SessionCheck]
    [HttpGet("MyProfile")]
    public IActionResult MyProfile() {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.Include(u => u.BooksPublished).FirstOrDefault(u => u.UserId == userId);

        return View(user);
    }

    [SessionCheck]
    [HttpPost]
    public IActionResult UploadProfilePicture(IFormFile profilePicture) {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        if (profilePicture != null && profilePicture.Length > 0) {
            using (var memoryStream = new MemoryStream())
            {
                profilePicture.CopyTo(memoryStream);
                user.ProfilePicture = memoryStream.ToArray();
                _context.SaveChanges();
            }
        }

        return RedirectToAction("MyProfile");
    }

    [SessionCheck]
    [HttpGet("EditProfile")]
    public IActionResult EditProfile() {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        return View("EditProfile", user);
    }

    [SessionCheck]
    [HttpPost]
    public IActionResult UpdateProfile(User updateUser) {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        // Validate specific fields
        if (string.IsNullOrWhiteSpace(updateUser.Name) || string.IsNullOrWhiteSpace(updateUser.LastName) || !IsValidEmail(updateUser.Email)) {
            ModelState.AddModelError("CustomError", "Invalid input for name, last name, or email.");
            return View("EditProfile", user);
        }

        user.Name = updateUser.Name;
        user.LastName = updateUser.LastName;
        user.Email = updateUser.Email;
        _context.SaveChanges();

        return RedirectToAction("MyProfile");
    }

    private bool IsValidEmail(string email) {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        } catch {
            return false;
        }
    }

    [SessionCheck]
    [HttpGet("BookDetails/{bookId}")]
    public IActionResult BookDetails(int bookId) {
        int? loggedInUserId = HttpContext.Session.GetInt32("UserId");
        var book = _context.Books.Include(b => b.BookAuthor).FirstOrDefault(b => b.BookId == bookId);

        ViewBag.IsBookOwner = book.UserId == loggedInUserId;

        return View(book);
    }

    [SessionCheck]
    [HttpGet("EditBook/{bookId}")]
    public IActionResult EditBook(int bookId) {
        int? loggedInUserId = HttpContext.Session.GetInt32("UserId");
        var book = _context.Books.FirstOrDefault(u => u.BookId == bookId);

        ViewBag.Genres = genres;
        return View(book);
    }

    [SessionCheck]
    [HttpPost("EditBook/{bookId}")]
    public IActionResult EditBook(int bookId, BookViewModel editedBook, IFormFile bookCover) {
        int? loggedInUserId = HttpContext.Session.GetInt32("UserId");
        var existingBook = _context.Books.FirstOrDefault(b => b.BookId == bookId);

        if (ModelState.IsValid) {
            existingBook.BookTitle = editedBook.BookTitle;
            existingBook.BookDescription = editedBook.BookDescription;
            existingBook.BookPages = editedBook.BookPages;
            existingBook.BookChapters = editedBook.BookChapters;
            existingBook.BookGenre = editedBook.BookGenre;

            if (bookCover != null) {
                existingBook.BookCover = GetByteArrayFromImage(bookCover);
            }

            _context.SaveChanges();

            return RedirectToAction("BookDetails", new { bookId });
        }

        ViewBag.Genres = genres;
        return View(editedBook);
    }

    private byte[] GetByteArrayFromImage(IFormFile image) {
        using (var memoryStream = new MemoryStream()) {
            image.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    [SessionCheck]
    [HttpGet("DeleteBook/{bookId}")]
    public IActionResult DeleteBook(int bookId) {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId && b.UserId == userId);

        _context.Books.Remove(book);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    [SessionCheck]
    [HttpGet("UserProfile/{id}")]
    public IActionResult UserProfile(int id) {
        var user = _context.Users.Include(u => u.BooksPublished).FirstOrDefault(u => u.UserId == id);

        return View(user);
    }

    [SessionCheck]
    [HttpGet("SurpriseMe")]
    public IActionResult SurpriseMe() {
        var books = _context.Books.Include(b => b.BookAuthor).ToList();
        Random random = new Random();
        int index = random.Next(books.Count);
        var book = books[index];

        return RedirectToAction("BookDetails", new { bookId = book.BookId });
    }

    [HttpGet("Logout")]
    public IActionResult Logout() {
        HttpContext.Session.Clear();
        return RedirectToAction("Auth");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}