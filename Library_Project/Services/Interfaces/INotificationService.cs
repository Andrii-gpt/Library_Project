namespace Library_Project.Services.Interfaces
{
    public interface INotificationService
    {
        void NotifyBorrow(int memberId, string title);
        void NotifyReturn(int memberId, string title);
    }
}
