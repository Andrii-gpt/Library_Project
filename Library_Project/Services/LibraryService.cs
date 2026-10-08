using Library_Project.Model;
using Library_Project.Services.Interfaces;

namespace Library_Project.Services
{
    public class LibraryService
    {
        private readonly IBookRepository _bookRepo;
        private readonly IMemberService _memberService;
        private readonly INotificationService _notification;

        public LibraryService(IBookRepository repo, IMemberService memberService, INotificationService notification)
        {
            _bookRepo = repo;
            _memberService = memberService;
            _notification = notification;
        }

        public void AddBook(string title, int copies)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title required.");
            if (copies <= 0)
                throw new ArgumentException("Copies must be positive.");

            var existing = _bookRepo.FindBook(title);
            if (existing == null)
            {
                _bookRepo.SaveBook(new Book { Title = title, Copies = copies });
            }
            else
            {
                existing.Copies += copies;
                _bookRepo.SaveBook(existing);
            }
        }

        public bool BorrowBook(int memberId, string title)
        {
            if (!_memberService.IsValidMember(memberId))
                throw new InvalidOperationException("Invalid member.");

            var book = _bookRepo.FindBook(title);
            if (book == null || book.Copies <= 0)
                return false;

            book.Copies--;
            _bookRepo.SaveBook(book);
            _notification.NotifyBorrow(memberId, title);
            return true;
        }

        public bool ReturnBook(int memberId, string title)
        {
            var book = _bookRepo.FindBook(title);
            if (book == null)
                return false;

            book.Copies++;
            _bookRepo.SaveBook(book);
            _notification.NotifyReturn(memberId, title);
            return true;
        }

        public List<Book> GetAvailableBooks()
        {
            var all = _bookRepo.GetAllBooks();
            return all.Where(b => b.Copies > 0).ToList();
        }
    }
}

