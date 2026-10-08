using Library_Project.Model;

namespace Library_Project.Services.Interfaces
{
    public interface IBookRepository
    {
        Book FindBook(string title);
        void SaveBook(Book book);
        List<Book> GetAllBooks();
    }

}
