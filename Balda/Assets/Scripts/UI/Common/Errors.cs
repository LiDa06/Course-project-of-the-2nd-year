using System;

namespace Balda.UI.Common
{
    /// <summary>
    /// Преобразует технические ошибки SDK/HTTP/БД в короткие сообщения,
    /// которые можно безопасно показывать пользователю в popup или inline-статусе.
    /// </summary>
    public static class Errors
    {
        public static string FromException(Exception ex, string fallback = "Произошла ошибка. Попробуй ещё раз.")
        {
            if (ex == null)
                return ForPopup(fallback);

            string full = ex.ToString();
            string message = ex.Message;

            string mapped = Map(full);
            if (!string.IsNullOrWhiteSpace(mapped))
                return mapped;

            mapped = Map(message);
            if (!string.IsNullOrWhiteSpace(mapped))
                return mapped;

            return ForPopup(fallback);
        }

        public static string ForPopup(string message, string fallback = "Произошла ошибка. Попробуй ещё раз.")
        {
            if (string.IsNullOrWhiteSpace(message))
                return fallback;

            string mapped = Map(message);
            if (!string.IsNullOrWhiteSpace(mapped))
                return mapped;

            string cleaned = message.Trim();

            if (cleaned.Equals("unknown error", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("неизвестная ошибка", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("неизвестная ошибка.", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            // Не показываем пользователю огромные технические JSON/stack trace.
            if (cleaned.Length > 180)
                return fallback;

            return cleaned;
        }

        public static bool IsNetworkError(Exception ex)
        {
            return ex != null && IsNetworkText(ex.ToString());
        }

        public static bool IsNetworkText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string text = raw.ToLowerInvariant();
            return text.Contains("httprequestexception") ||
                   text.Contains("webexception") ||
                   text.Contains("transport connection") ||
                   text.Contains("forcibly closed") ||
                   text.Contains("timed out") ||
                   text.Contains("timeout") ||
                   text.Contains("connection") ||
                   text.Contains("network") ||
                   text.Contains("sending the request") ||
                   text.Contains("name resolution") ||
                   text.Contains("failed to connect") ||
                   text.Contains("no route") ||
                   text.Contains("internet");
        }

        private static string Map(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string text = raw.ToLowerInvariant();

            if (IsNetworkText(raw))
                return "Не удалось связаться с сервером. Проверь подключение к интернету и попробуй ещё раз.";

            if (text.Contains("user_not_found") ||
                text.Contains("user not found") ||
                text.Contains("not found") && text.Contains("user") ||
                text.Contains("signups not allowed") ||
                text.Contains("signup is disabled") ||
                text.Contains("signup disabled") ||
                text.Contains("shouldcreateuser") ||
                text.Contains("for security purposes") && text.Contains("only request this after"))
            {
                return "Аккаунт с такой почтой не найден. Проверь email или зарегистрируйся.";
            }

            if (text.Contains("otp_expired") || text.Contains("token has expired") || text.Contains("expired"))
                return "Срок действия кода истёк. Запроси новый код.";

            if (text.Contains("invalid token") ||
                text.Contains("token is invalid") ||
                text.Contains("invalid otp") ||
                text.Contains("otp invalid") ||
                text.Contains("invalid_grant") ||
                text.Contains("invalid login credentials") ||
                text.Contains("bad jwt") ||
                text.Contains("invalid"))
            {
                return "Неверный код. Проверь письмо и введи 6 цифр без пробелов.";
            }

            if (text.Contains("over_email_send_rate_limit") ||
                text.Contains("rate limit") ||
                text.Contains("too many requests") ||
                text.Contains("email rate limit exceeded"))
            {
                return "Код запрашивается слишком часто. Подожди немного и попробуй снова.";
            }

            if (text.Contains("user already registered") ||
                text.Contains("email_exists") ||
                text.Contains("already registered") ||
                text.Contains("already exists") && text.Contains("email"))
            {
                return "Пользователь с такой почтой уже существует. Попробуй войти в аккаунт.";
            }

            if (text.Contains("username_taken") ||
                text.Contains("duplicate key") && text.Contains("username") ||
                text.Contains("username") && text.Contains("already"))
            {
                return "Это имя пользователя уже занято.";
            }

            if (text.Contains("duplicate") || text.Contains("unique constraint") || text.Contains("23505"))
                return "Такое значение уже занято.";

            if (text.Contains("not_authenticated") ||
                text.Contains("jwt") && text.Contains("missing") ||
                text.Contains("unauthorized") ||
                text.Contains("401"))
            {
                return "Сессия истекла. Войди в аккаунт заново.";
            }

            if (text.Contains("permission denied") ||
                text.Contains("row-level security") ||
                text.Contains("violates row-level security") ||
                text.Contains("403"))
            {
                return "Недостаточно прав для выполнения операции. Попробуй войти в аккаунт заново.";
            }

            if (text.Contains("email not confirmed"))
                return "Почта ещё не подтверждена. Введи код из письма.";

            if (text.Contains("email") && text.Contains("invalid"))
                return "Некорректный формат email.";

            if (text.Contains("account") && text.Contains("deleted"))
                return "Этот аккаунт удалён.";

            if (text.Contains("unknown error"))
                return "Произошла ошибка. Попробуй ещё раз.";

            return null;
        }
    }
}
