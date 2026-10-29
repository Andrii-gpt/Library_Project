using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        private const string BOOK_TITLE = "Clean Code";
        private const int MEMBER_ID = 1;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        /// <summary>
        /// Перевіряє, що AddBook кидає ArgumentException, якщо назва книги null, порожня або з пробілів.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddBook_EmptyTitle_ThrowsArgumentException(string? title)
        {
            const int COPIES = 1;
            Action act = () => _service.AddBook(title!, COPIES);

            Assert.Throws<ArgumentException>(act);
        }

        /// <summary>
        /// Перевіряє, що при нульовій або від'ємній кількості примірників книга не зберігається в репозиторій.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void AddBook_NonPositiveCopies_NeverSaves(int copies)
        {
            Assert.Throws<ArgumentException>(() => _service.AddBook(BOOK_TITLE, copies));

            _repoMock.Verify(r => r.SaveBook(It.IsAny<Book>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє, що нова книга зберігається з тією назвою і кількістю примірників, які передали в AddBook.
        /// </summary>
        [Fact]
        public void AddBook_NewBook_SavesBookWithCorrectData()
        {
            const int COPIES = 3;
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns((Book)null!);

            _service.AddBook(BOOK_TITLE, COPIES);

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == BOOK_TITLE && b.Copies == COPIES)), Times.Once);
        }

        /// <summary>
        /// Перевіряє, що для книги, яка вже є в бібліотеці, AddBook збільшує кількість примірників.
        /// </summary>
        [Fact]
        public void AddBook_ExistingBook_IncreasesCopies()
        {
            var existing = new Book { Title = BOOK_TITLE, Copies = 2 };
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns(existing);

            _service.AddBook(BOOK_TITLE, 3);

            Assert.Equal(5, existing.Copies);
        }

        /// <summary>
        /// Перевіряє, що BorrowBook повертає true, коли учасник дійсний і книга є в наявності.
        /// </summary>
        [Fact]
        public void BorrowBook_AvailableBook_ReturnsTrue()
        {
            _memberMock.Setup(m => m.IsValidMember(MEMBER_ID)).Returns(true);
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns(new Book { Title = BOOK_TITLE, Copies = 1 });

            var result = _service.BorrowBook(MEMBER_ID, BOOK_TITLE);

            Assert.True(result);
        }

        /// <summary>
        /// Перевіряє, що BorrowBook повертає false, якщо книги немає в репозиторії.
        /// </summary>
        [Fact]
        public void BorrowBook_BookNotFound_ReturnsFalse()
        {
            _memberMock.Setup(m => m.IsValidMember(MEMBER_ID)).Returns(true);
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns((Book)null!);

            var result = _service.BorrowBook(MEMBER_ID, BOOK_TITLE);

            Assert.False(result);
        }

        /// <summary>
        /// Перевіряє, що після видачі книги кількість примірників змінюється (зменшується).
        /// </summary>
        [Fact]
        public void BorrowBook_AvailableBook_DecreasesCopies()
        {
            const int INITIAL_COPIES = 2;
            var book = new Book { Title = BOOK_TITLE, Copies = INITIAL_COPIES };
            _memberMock.Setup(m => m.IsValidMember(MEMBER_ID)).Returns(true);
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns(book);

            _service.BorrowBook(MEMBER_ID, BOOK_TITLE);

            Assert.NotEqual(INITIAL_COPIES, book.Copies);
        }

        /// <summary>
        /// Перевіряє, що дві видачі книги викликають SaveBook рівно два рази.
        /// </summary>
        [Fact]
        public void BorrowBook_TwoBorrows_SavesTwice()
        {
            var book = new Book { Title = BOOK_TITLE, Copies = 2 };
            _memberMock.Setup(m => m.IsValidMember(MEMBER_ID)).Returns(true);
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns(book);

            _service.BorrowBook(MEMBER_ID, BOOK_TITLE);
            _service.BorrowBook(MEMBER_ID, BOOK_TITLE);

            _repoMock.Verify(r => r.SaveBook(book), Times.Exactly(2));
        }

        /// <summary>
        /// Перевіряє, що при поверненні наявної книги хоча б раз надсилається сповіщення NotifyReturn.
        /// </summary>
        [Fact]
        public void ReturnBook_ExistingBook_NotifiesReturn()
        {
            _repoMock.Setup(r => r.FindBook(BOOK_TITLE)).Returns(new Book { Title = BOOK_TITLE, Copies = 0 });

            _service.ReturnBook(MEMBER_ID, BOOK_TITLE);

            _notifMock.Verify(n => n.NotifyReturn(It.IsAny<int>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Перевіряє, що GetAvailableBooks повертає список, у якому є книга з примірниками в наявності.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_MixedCopies_ContainsAvailableBook()
        {
            var available = new Book { Title = BOOK_TITLE, Copies = 3 };
            var unavailable = new Book { Title = "Refactoring", Copies = 0 };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(new List<Book> { available, unavailable });

            var result = _service.GetAvailableBooks();

            Assert.NotNull(result);
            Assert.Contains(available, result);
        }

        /// <summary>
        /// Перевіряє, що GetAvailableBooks повертає порожній список, якщо в усіх книг 0 примірників.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_AllZeroCopies_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetAllBooks()).Returns(new List<Book>
            {
                new Book { Title = BOOK_TITLE, Copies = 0 },
                new Book { Title = "Refactoring", Copies = 0 }
            });

            var result = _service.GetAvailableBooks();

            Assert.Empty(result);
        }
    }
}