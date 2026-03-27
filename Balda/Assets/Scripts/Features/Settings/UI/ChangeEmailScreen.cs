using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Balda.UI.Common;
using Balda.Core.Navigation;
using TMPro;
using UnityEngine;
using Balda.Infrastructure.LocalStorage;

namespace Balda.Features.Settings.UI
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
