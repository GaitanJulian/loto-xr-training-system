using UnityEngine;
using Project.Procedure;

namespace Project.Actions.Debug
{
    public sealed class ActionDebugUI : MonoBehaviour
    {
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TargetId target = TargetId.Breaker_01;

        public void ToggleBreakerOff() => runner.PublishAction(new ActionEvent(ActionType.ToggleBreakerOff, target));
        public void ApplyLock() => runner.PublishAction(new ActionEvent(ActionType.ApplyLock, target));
        public void AttachTag() => runner.PublishAction(new ActionEvent(ActionType.AttachTag, target));
        public void TryStart() => runner.PublishAction(new ActionEvent(ActionType.TryStart, target));
    }
}
