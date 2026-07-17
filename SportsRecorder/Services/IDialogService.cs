namespace SportsRecorder.Services
{
    public interface IDialogService
    {
        bool Confirm(string message, string title = "確認");
        bool ShowMessage(string message, string title = "成功");

        string? Prompt(string message, string defaultValue = "", string title = "請輸入");
    }
}
