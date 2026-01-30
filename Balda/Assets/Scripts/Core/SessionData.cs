using System;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public static class SessionData
    {
        public static string Name = "Guest";
        public static string Email = "";
        public static DateTime RegistrationDate;
        public static DateTime TimeInGame;

        public static int FieldSize = 5;
        public static int numberOfGames = 0;
        public static int numberOfWins = 0;
        public static int numberOfLoss = 0;

        //Может и не понадобятся
        public static int userLevel = 1;
        public static int userXP = 0;
    }
}
