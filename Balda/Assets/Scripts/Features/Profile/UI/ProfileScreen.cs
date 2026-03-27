using Balda.UI.Common;
using Balda.Core.Navigation;
using TMPro;
using UnityEngine;
using Balda.Infrastructure.LocalStorage;
using Balda.Features.MainMenu.UI;

namespace Balda.Features.Profile.UI
{
    public class ProfileScreen : ScreenBase
    {
        [SerializeField] private TMP_Text email;
        [SerializeField] private TMP_Text dateOfRegistration;
        [SerializeField] private TMP_Text timeInGame;

        private void OnEnable()
        {
            email.text = LocalPlayerData.Instance.Email;
            dateOfRegistration.text = LocalPlayerData.Instance.CreatedAtTicks.ToString();
            timeInGame.text = ""; //////////////////////////
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
