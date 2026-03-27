using Balda.UI.Common;
using Balda.Core.Navigation;
using TMPro;
using UnityEngine;
using Balda.Infrastructure.LocalStorage;

namespace Balda.Features.Settings.UI
{
    public class ChangeNameScreen : ScreenBase
    {
        [SerializeField] private TMP_InputField newNameInput;

        public void OnEnable()
        {
            newNameInput.text = LocalPlayerData.Instance.LocalDisplayName;
        }

        public void OnSaveClick()
        {
            LocalPlayerData.Instance.LocalDisplayName = newNameInput.text;
        }
        public void OnBack()
        {
            ScreenRouter.Instance.Show<SettingsScreen>();
        }
    }
}
