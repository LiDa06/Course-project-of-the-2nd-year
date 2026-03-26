using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.App;
using Assets.Scripts.LocalData;
using Assets.Scripts.UI.Screens.Settings;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens
{
    public class ChangeEmailScreen : ScreenBase
    {
        [SerializeField] private TMP_InputField newEmailInput;

        public void OnEnable()
        {
            newEmailInput.text = LocalPlayerData.Instance.Email;
        }

        public void OnContinueClick()
        {
            LocalPlayerData.Instance.Email = newEmailInput.text;
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<SettingsScreen>();
        }
    }
}
