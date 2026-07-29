using System.Collections.Generic;

namespace Server.Custom.AIGM.UMG
{
    public sealed class UMGCompanionAbilityBlock
    {
        public string Id;
        public string Character;
        public string Ability;
        public string Trigger;
        public string Directive;
        public string TargetPriority;
        public List<string> Constraints;
        public string PersonaBias;
        public List<string> Tags;

        public UMGCompanionAbilityBlock(string id, string character, string ability, string trigger, string directive, string targetPriority, string[] constraints, string personaBias, string[] tags)
        {
            Id = id;
            Character = character;
            Ability = ability;
            Trigger = trigger;
            Directive = directive;
            TargetPriority = targetPriority;
            Constraints = new List<string>(constraints ?? new string[0]);
            PersonaBias = personaBias;
            Tags = new List<string>(tags ?? new string[0]);
        }

        public string FormatCompact()
        {
            return Id + "/" + Ability + ": " + Directive + " Trigger=" + Trigger + " Target=" + TargetPriority + " Persona=" + PersonaBias;
        }
    }
}
