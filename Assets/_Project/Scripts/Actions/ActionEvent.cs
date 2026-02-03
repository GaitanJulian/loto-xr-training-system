namespace Project.Actions
{
    public readonly struct ActionEvent
    {
        public readonly ActionType Type;
        public readonly string TargetId;

        public ActionEvent(ActionType type, string targetId)
        {
            Type = type;
            TargetId = targetId;
        }

        public override string ToString()
        {
            return $"ActionEvent: {Type} on {TargetId}";
        }
    }
}
