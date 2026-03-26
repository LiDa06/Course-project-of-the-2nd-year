using Assets.Scripts.App;
using Assets.Scripts.LocalData;
using Assets.Scripts.UI.Screens.Settings;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Screens
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
