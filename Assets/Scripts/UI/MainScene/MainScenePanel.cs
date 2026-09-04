using Framework;
using GamePlay.Procedure;

namespace UI.MainScene
{
    public class MainScenePanel : UnityEngine.MonoBehaviour
    {
        public void UI_LocalPlay()
        {
            ProcedureCore.Instance.StartLocal();
        }

        public void UI_MultiPlay()
        {
            ProcedureCore.Instance.HostMultiplayer();
        }

        public void UI_Join()
        {
            ProcedureCore.Instance.JoinRemote();
        }

        public void UI_Exit()
        {
            Global.QuitGame();
        }
    }
}
