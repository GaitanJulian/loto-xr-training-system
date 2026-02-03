using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.Actions.Debug
{
    public sealed class ActionDebugUI : MonoBehaviour
    {
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private string targetId = "breaker_01";

        public void ToggleBreakerOff()
        {
            runner.PublishAction(
                new ActionEvent(ActionType.ToggleBreakerOff, targetId)
            );
        }

        public void ApplyLock()
        {
            runner.PublishAction(
                new ActionEvent(ActionType.ApplyLock, targetId)
            );
        }

        public void AttachTag()
        {
            runner.PublishAction(
                new ActionEvent(ActionType.AttachTag, targetId)
            );
        }

        public void TryStart()
        {
            runner.PublishAction(
                new ActionEvent(ActionType.TryStart, targetId)
            );
        }
    }
}
