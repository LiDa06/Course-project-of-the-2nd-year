using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Balda.UI.Common;
using Balda.Core.Navigation;
using Balda.Features.MainMenu.UI;

namespace Balda.Features.FriendGame.UI
{
    public class PlayWithFriendScreen : ScreenBase
    {
        public void OnBack()
        {
            ScreenRouter.Instance.Show<MainScreen>();
        }
    }
}
