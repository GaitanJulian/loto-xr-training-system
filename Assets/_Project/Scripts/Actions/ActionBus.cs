using System;

namespace Project.Actions
{
    public sealed class ActionBus
    {
        public event Action<ActionEvent> OnAction;

        public void Publish(ActionEvent actionEvent)
        {
            OnAction?.Invoke(actionEvent);
        }
    }
}
