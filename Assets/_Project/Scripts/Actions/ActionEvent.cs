namespace Project.Actions
{
    public readonly struct ActionEvent
    {
        public readonly ActionType Type;
        public readonly TargetId Target;

        public ActionEvent(ActionType type, TargetId target)
        {
            Type = type;
            Target = target;
        }

        public override string ToString()
        {
            return $"ActionEvent: {Type} on {Target}";
        }
    }
}
