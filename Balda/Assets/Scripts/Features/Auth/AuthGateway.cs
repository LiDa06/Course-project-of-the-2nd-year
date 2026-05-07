using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Balda.Features.Auth
{
    public static class AuthGateway
    {
        public static bool HasAuthService()
        {
            return GetAuthService() != null;
        }

        public static Task ChangeUsernameAsync(string newUsername)
        {
            return InvokeTaskAsync("ChangeUsernameAsync", new object[] { newUsername });
        }

        public static Task BeginEmailChangeAsync(string newEmail)
        {
            return InvokeTaskAsync("BeginEmailChangeAsync", new object[] { newEmail });
        }

        public static Task SignOutAsync()
        {
            return InvokeTaskAsync("SignOutAsync", Array.Empty<object>());
        }

        private static async Task InvokeTaskAsync(string methodName, object[] args)
        {
            object authService = GetAuthService();
            if (authService == null)
                throw new InvalidOperationException("Не удалось найти auth service в проекте.");

            MethodInfo method = authService.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == methodName &&
                    m.GetParameters().Length == args.Length);

            if (method == null)
                throw new MissingMethodException(authService.GetType().Name, methodName);

            object result = method.Invoke(authService, args);

            if (result is Task task)
            {
                await task;
                return;
            }

            throw new InvalidOperationException($"Метод {methodName} не вернул Task.");
        }

        private static object GetAuthService()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.Name != "SupabaseManager")
                        continue;

                    object managerInstance = GetSingletonInstance(type);
                    if (managerInstance == null)
                        continue;

                    object authService = GetMemberValue(managerInstance, type, "AuthService")
                                         ?? GetMemberValue(managerInstance, type, "Auth")
                                         ?? GetMemberValue(managerInstance, type, "SupabaseAuthService");

                    if (authService != null)
                        return authService;
                }
            }

            Debug.LogWarning("AuthGateway: SupabaseManager/AuthService не найден.");
            return null;
        }

        private static object GetSingletonInstance(Type type)
        {
            object instance =
                GetStaticMemberValue(type, "Instance") ??
                GetStaticMemberValue(type, "instance") ??
                GetStaticMemberValue(type, "Current");

            return instance;
        }

        private static object GetStaticMemberValue(Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
                return property.GetValue(null);

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
                return field.GetValue(null);

            return null;
        }

        private static object GetMemberValue(object instance, Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
                return property.GetValue(instance);

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(instance);

            return null;
        }
    }
}