using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.App;
using Assets.Scripts.UI.Screens.Main;

namespace Assets.Scripts.UI.Screens
{
    public class RulesScreen : ScreenBase
    {
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
