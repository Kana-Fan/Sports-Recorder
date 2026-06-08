namespace SportsRecorder.Services
{
    public interface IDialogService
    {
        bool Confirm(string message, string title = "確認");
        bool ShowMessage(string message, string title = "成功");
    }
}
