namespace Balda.Features.Settings.UI
{
    public enum PendingAccountActionType
    {
        None,
        ChangeName,
        ChangeEmail
    }

    /// <summary>
    /// Хранит действие, которое пользователь начал из настроек, но для него понадобилось
    /// заново подтвердить вход по коду. Это нужно, чтобы после ввода кода действие
    /// выполнилось автоматически, а введённое новое имя/почта не потерялись.
    /// </summary>
    public static class PendingAccountAction
    {
        public static PendingAccountActionType Type { get; private set; } = PendingAccountActionType.None;
        public static string NewUsername { get; private set; } = string.Empty;
        public static string NewEmail { get; private set; } = string.Empty;

        public static bool HasPending => Type != PendingAccountActionType.None;
        public static bool HasChangeName => Type == PendingAccountActionType.ChangeName;
        public static bool HasChangeEmail => Type == PendingAccountActionType.ChangeEmail;

        public static void SetChangeName(string newUsername)
        {
            Type = PendingAccountActionType.ChangeName;
            NewUsername = newUsername ?? string.Empty;
            NewEmail = string.Empty;
        }

        public static void SetChangeEmail(string newEmail)
        {
            Type = PendingAccountActionType.ChangeEmail;
            NewEmail = newEmail ?? string.Empty;
            NewUsername = string.Empty;
        }

        public static void Clear()
        {
            Type = PendingAccountActionType.None;
            NewUsername = string.Empty;
            NewEmail = string.Empty;
        }
    }
}
