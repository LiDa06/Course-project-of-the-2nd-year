using Assets.Scripts.App;
using Assets.Scripts.Data;
using UnityEngine;
using Assets.Scripts.UI.Screens.Main;

namespace Assets.Scripts.UI.Screens.Settings
{
    public class SettingsScreen : ScreenBase
    {
        [SerializeField] private SwitchThemeModeBox ThemeModeBox;
        [SerializeField] private SwitchVolumeBox VolumeBox;
        public void ThemeModeClick(bool isOn)
        {
            ThemeModeBox.Switch(isOn);
        }

        public void VolumeClick(bool isOn)
        {
            VolumeBox.Switch(isOn);
        }

        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
