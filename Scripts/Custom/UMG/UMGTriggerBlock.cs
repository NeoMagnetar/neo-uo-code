namespace Server.Custom.UMG
{
    public sealed class UMGTriggerBlock
    {
        public string Id { get; set; }
        public string TriggerKind { get; set; }
        public string[] Keywords { get; set; }
        public int Priority { get; set; }
        public int CooldownSeconds { get; set; }
        public string Primary { get; set; }
        public string Directive { get; set; }
        public string Instruction { get; set; }
        public string OffRule { get; set; }
        public string ModeTag { get; set; }
    }
}
