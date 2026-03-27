using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Balda.UI.Common;
using Balda.Core.Navigation;

namespace Balda.Features.MainMenu.UI
{
    public class RulesScreen : ScreenBase
    {
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
