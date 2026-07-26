using System;
using Server.Accounting;
using Server.Custom.AIGM;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Misc
{
    public class Profile
    {
        public static void Initialize()
        {
            EventSink.ProfileRequest += new ProfileRequestEventHandler(EventSink_ProfileRequest);
            EventSink.ChangeProfileRequest += new ChangeProfileRequestEventHandler(EventSink_ChangeProfileRequest);
        }

        private static bool IsEditableCompanionJournal(Mobile beholder, Mobile beheld)
        {
            if (beholder == null || beheld == null)
                return false;

            if (beholder == beheld)
                return true;

            if (beholder.AccessLevel > beheld.AccessLevel)
                return true;

            BaseHire hire = beheld as BaseHire;

            return hire != null
                && hire.Controlled
                && hire.ControlMaster == beholder
                && beheld is IAIGMCompanionActor;
        }

        private static bool CanViewProfile(Mobile beholder, Mobile beheld)
        {
            if (beholder == null || beheld == null)
                return false;

            if (beheld.Player)
                return true;

            return IsEditableCompanionJournal(beholder, beheld);
        }

        public static void EventSink_ChangeProfileRequest(ChangeProfileRequestEventArgs e)
        {
            if (!IsEditableCompanionJournal(e.Beholder, e.Beheld))
            {
                e.Beholder.SendMessage("You do not have permission to do that.");
                return;
            }
            
            Mobile from = e.Beholder;
            Mobile target = e.Beheld;
            PlayerMobile player = target as PlayerMobile;

            if (target.ProfileLocked)
                from.SendMessage("Your profile is locked. You may not change it.");
            else
            {
                string text = e.Text ?? String.Empty;

                if (player != null)
                    player.PaperdollJournal = text;

                target.Profile = text;
            }
        }

        public static void EventSink_ProfileRequest(ProfileRequestEventArgs e)
        {
            Mobile beholder = e.Beholder;
            Mobile beheld = e.Beheld;

            if (!CanViewProfile(beholder, beheld))
                return;

            if (beholder.Map != beheld.Map || !beholder.InRange(beheld, 12) || !beholder.CanSee(beheld))
                return;

            if (beheld is IAIGMCompanionActor)
                AIGMCompanionMemoryLoader.EnsureJournalLoaded(beheld);

            string header = Titles.ComputeTitle(beholder, beheld);

            string footer = "";

            if (beheld.ProfileLocked)
            {
                if (beholder == beheld)
                    footer = "Your profile has been locked.";
                else if (beholder.IsStaff())
                    footer = "This profile has been locked.";
            }

            if (footer.Length == 0 && beholder == beheld)
                footer = GetAccountDuration(beheld);
            else if (footer.Length == 0 && beheld is IAIGMCompanionActor && IsEditableCompanionJournal(beholder, beheld))
                footer = "Companion neosleeve journal. Saved with this companion and loaded into runtime memory.";

            string body = beheld.Profile;
            PlayerMobile player = beheld as PlayerMobile;

            if (player != null)
            {
                body = player.PaperdollJournal;

                if ((body == null || body.Length == 0) && !String.IsNullOrEmpty(beheld.Profile))
                {
                    player.PaperdollJournal = beheld.Profile;
                    body = player.PaperdollJournal;
                }
            }

            if (body == null || body.Length <= 0)
                body = "";

            if (beheld is IAIGMCompanionActor && IsEditableCompanionJournal(beholder, beheld))
            {
                beholder.CloseGump(typeof(CompanionJournalGump));
                beholder.SendGump(new CompanionJournalGump(beholder, beheld, body));
                return;
            }

            beholder.Send(new DisplayProfile(IsEditableCompanionJournal(beholder, beheld) || (beholder != beheld || !beheld.ProfileLocked), beheld, header, body, footer));
        }

        public static bool Format(double value, string format, out string op)
        {
            if (value >= 1.0)
            {
                op = String.Format(format, (int)value, (int)value != 1 ? "s" : "");
                return true;
            }

            op = null;
            return false;
        }

        private static string GetAccountDuration(Mobile m)
        {
            Account a = m.Account as Account;

            if (a == null)
                return "";

            TimeSpan ts = DateTime.UtcNow - a.Created;

            string v;

            if (Format(ts.TotalDays, "This account is {0} day{1} old.", out v))
                return v;

            if (Format(ts.TotalHours, "This account is {0} hour{1} old.", out v))
                return v;

            if (Format(ts.TotalMinutes, "This account is {0} minute{1} old.", out v))
                return v;

            if (Format(ts.TotalSeconds, "This account is {0} second{1} old.", out v))
                return v;

            return "";
        }

        private static void SaveCompanionJournal(Mobile beholder, Mobile beheld, string text)
        {
            if (!IsEditableCompanionJournal(beholder, beheld) || beheld == null)
                return;

            string normalized = NormalizeJournalText(text);
            PlayerMobile player = beheld as PlayerMobile;

            if (player != null)
                player.PaperdollJournal = normalized;

            beheld.Profile = normalized;
        }

        private static string NormalizeJournalText(string text)
        {
            text = text ?? String.Empty;
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            if (text.Length > 8000)
                text = text.Substring(0, 8000);

            return text.Trim();
        }

        private static string AppendTemplate(string current, string template)
        {
            current = NormalizeJournalText(current);
            template = NormalizeJournalText(template);

            if (current.Length == 0)
                return template;

            if (template.Length == 0)
                return current;

            return NormalizeJournalText(current + "\n\n" + template);
        }

        private sealed class CompanionJournalGump : Gump
        {
            private const int EntryId = 1;
            private const int EditPageButtonId = 7;
            private const int PreviousPageButtonId = 8;
            private const int NextPageButtonId = 9;
            private const int DoneEditingButtonId = 10;
            private const int PageCharacterLimit = 1900;
            private readonly Mobile _beholder;
            private readonly Mobile _beheld;
            private readonly string _body;
            private readonly int _pageIndex;
            private readonly bool _editing;

            public CompanionJournalGump(Mobile beholder, Mobile beheld, string body)
                : this(beholder, beheld, body, 0, false)
            {
            }

            public CompanionJournalGump(Mobile beholder, Mobile beheld, string body, int pageIndex)
                : this(beholder, beheld, body, pageIndex, false)
            {
            }

            public CompanionJournalGump(Mobile beholder, Mobile beheld, string body, int pageIndex, bool editing)
                : base(80, 60)
            {
                _beholder = beholder;
                _beheld = beheld;
                _body = NormalizeJournalText(body);
                _pageIndex = GetClampedPageIndex(_body, pageIndex);
                _editing = editing;

                Closable = true;
                Disposable = true;
                Dragable = true;
                Resizable = false;

                AddPage(0);
                AddBackground(0, 0, 780, 560, 5054);
                AddImageTiled(15, 15, 750, 530, 2624);
                AddHtml(25, 20, 730, 24, "<BASEFONT COLOR=#3B2C1A><CENTER>Companion Neosleeve Journal</CENTER></BASEFONT>", false, false);
                AddHtml(25, 48, 730, 38, String.Format("<BASEFONT COLOR=#4A3721>{0}</BASEFONT>", _beheld != null ? _beheld.Name : "Companion"), false, false);
                AddHtml(25, 74, 730, 40, "<BASEFONT COLOR=#5A4A34>Use this journal to shape persona, sleeve structure, UMG blocks, and operating notes. Save writes directly into companion runtime memory.</BASEFONT>", false, false);

                AddButton(24, 118, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtml(58, 118, 100, 20, "<BASEFONT COLOR=#3B2C1A>Save</BASEFONT>", false, false);

                AddButton(120, 118, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddHtml(154, 118, 130, 20, "<BASEFONT COLOR=#3B2C1A>Save + Close</BASEFONT>", false, false);

                AddButton(292, 118, 4005, 4007, 3, GumpButtonType.Reply, 0);
                AddHtml(326, 118, 120, 20, "<BASEFONT COLOR=#3B2C1A>Insert Sleeve</BASEFONT>", false, false);

                AddButton(444, 118, 4005, 4007, 4, GumpButtonType.Reply, 0);
                AddHtml(478, 118, 120, 20, "<BASEFONT COLOR=#3B2C1A>Insert Persona</BASEFONT>", false, false);

                AddButton(24, 146, 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddHtml(58, 146, 180, 20, "<BASEFONT COLOR=#3B2C1A>Insert Logic Stack</BASEFONT>", false, false);

                AddButton(240, 146, 4005, 4007, 6, GumpButtonType.Reply, 0);
                AddHtml(274, 146, 180, 20, "<BASEFONT COLOR=#3B2C1A>Prompt Append</BASEFONT>", false, false);

                int pageCount = GetPageCount(_body);
                AddButton(610, 146, 4014, 4016, PreviousPageButtonId, GumpButtonType.Reply, 0);
                AddHtml(642, 146, 28, 20, "<BASEFONT COLOR=#3B2C1A><</BASEFONT>", false, false);
                AddHtml(670, 146, 70, 20, String.Format("<BASEFONT COLOR=#3B2C1A>Page {0}/{1}</BASEFONT>", _pageIndex + 1, pageCount), false, false);
                AddButton(736, 146, 4005, 4007, NextPageButtonId, GumpButtonType.Reply, 0);
                AddHtml(768, 146, 18, 20, "<BASEFONT COLOR=#3B2C1A>></BASEFONT>", false, false);

                AddHtml(24, 174, 720, 20, String.Format("<BASEFONT COLOR=#3B2C1A>{0}</BASEFONT>", _editing ? "Edit page" : "Journal"), false, false);

                if (_editing)
                {
                    AddButton(630, 174, 4005, 4007, DoneEditingButtonId, GumpButtonType.Reply, 0);
                    AddHtml(664, 174, 100, 20, "<BASEFONT COLOR=#3B2C1A>Done Editing</BASEFONT>", false, false);
                    AddImageTiled(22, 196, 722, 308, 2624);
                    AddTextEntry(30, 204, 706, 292, 0x497, EntryId, GetPageText(_body, _pageIndex), PageCharacterLimit);
                    AddHtml(24, 514, 720, 24, "<BASEFONT COLOR=#5A4A34>Edit this page, then Save. Use page arrows to move between sections.</BASEFONT>", false, false);
                }
                else
                {
                    AddButton(630, 174, 4005, 4007, EditPageButtonId, GumpButtonType.Reply, 0);
                    AddHtml(664, 174, 80, 20, "<BASEFONT COLOR=#3B2C1A>Edit Page</BASEFONT>", false, false);
                    AddImageTiled(22, 196, 722, 308, 2624);
                    AddHtml(30, 204, 706, 292, BuildJournalPreviewHtml(_body), true, true);
                    AddHtml(24, 514, 720, 24, "<BASEFONT COLOR=#5A4A34>Click inside the journal and use mouse wheel or arrow keys to scroll.</BASEFONT>", false, false);
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (_beholder == null || _beheld == null || state == null || state.Mobile != _beholder)
                    return;

                TextRelay relay = info.GetTextEntry(EntryId);
                string text = _editing && relay != null ? relay.Text : GetPageText(_body, _pageIndex);
                string updatedBody = ReplacePageText(_body, _pageIndex, text);

                switch (info.ButtonID)
                {
                    case 1:
                        SaveCompanionJournal(_beholder, _beheld, updatedBody);
                        _beholder.SendMessage("Companion journal saved.");
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, updatedBody, _pageIndex, _editing));
                        break;
                    case 2:
                        SaveCompanionJournal(_beholder, _beheld, updatedBody);
                        _beholder.SendMessage("Companion journal saved.");
                        break;
                    case 3:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, AppendTemplate(updatedBody, SleeveTemplate), GetLastPageIndex(AppendTemplate(updatedBody, SleeveTemplate)), _editing));
                        break;
                    case 4:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, AppendTemplate(updatedBody, PersonaTemplate), GetLastPageIndex(AppendTemplate(updatedBody, PersonaTemplate)), _editing));
                        break;
                    case 5:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, AppendTemplate(updatedBody, LogicStackTemplate), GetLastPageIndex(AppendTemplate(updatedBody, LogicStackTemplate)), _editing));
                        break;
                    case 6:
                        _beholder.Prompt = new CompanionJournalAppendPrompt(_beholder, _beheld, updatedBody);
                        _beholder.SendMessage("Enter the text to append to this companion journal.");
                        break;
                    case EditPageButtonId:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, updatedBody, _pageIndex, true));
                        break;
                    case PreviousPageButtonId:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, updatedBody, _pageIndex - 1, _editing));
                        break;
                    case NextPageButtonId:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, updatedBody, _pageIndex + 1, _editing));
                        break;
                    case DoneEditingButtonId:
                        _beholder.SendGump(new CompanionJournalGump(_beholder, _beheld, updatedBody, _pageIndex, false));
                        break;
                }
            }

            private static int GetPageCount(string text)
            {
                int length = String.IsNullOrEmpty(text) ? 0 : text.Length;
                return Math.Max(1, (length + PageCharacterLimit - 1) / PageCharacterLimit);
            }

            private static int GetClampedPageIndex(string text, int pageIndex)
            {
                return Math.Max(0, Math.Min(GetPageCount(text) - 1, pageIndex));
            }

            private static int GetLastPageIndex(string text)
            {
                return GetPageCount(text) - 1;
            }

            private static string GetPageText(string text, int pageIndex)
            {
                text = NormalizeJournalText(text);
                pageIndex = GetClampedPageIndex(text, pageIndex);

                int start = pageIndex * PageCharacterLimit;

                if (start >= text.Length)
                    return String.Empty;

                int length = Math.Min(PageCharacterLimit, text.Length - start);
                return text.Substring(start, length);
            }

            private static string ReplacePageText(string fullText, int pageIndex, string pageText)
            {
                fullText = NormalizeJournalText(fullText);
                pageText = NormalizeJournalText(pageText);
                pageIndex = GetClampedPageIndex(fullText, pageIndex);

                int start = pageIndex * PageCharacterLimit;

                if (start > fullText.Length)
                    start = fullText.Length;

                int existingLength = 0;

                if (start < fullText.Length)
                    existingLength = Math.Min(PageCharacterLimit, fullText.Length - start);

                string prefix = start > 0 ? fullText.Substring(0, start) : String.Empty;
                string suffix = start + existingLength < fullText.Length ? fullText.Substring(start + existingLength) : String.Empty;

                return NormalizeJournalText(prefix + pageText + suffix);
            }

            private static string BuildJournalPreviewHtml(string text)
            {
                string normalized = NormalizeJournalText(text);

                if (normalized.Length == 0)
                    return "<BASEFONT COLOR=#5A4A34><I>Journal is empty.</I></BASEFONT>";

                return String.Format(
                    "<BASEFONT COLOR=#2B2118>{0}</BASEFONT>",
                    EncodeHtml(normalized).Replace("\n", "<BR>")
                );
            }

            private static string EncodeHtml(string text)
            {
                if (String.IsNullOrEmpty(text))
                    return String.Empty;

                return text
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
            }

            private const string SleeveTemplate =
@"[NEOSLEEVE]
name:
role:
purpose:

[DIRECTIVE]

[INSTRUCTION]

[SUBJECT]

[PRIMARY]

[PHILOSOPHY]

[BLUEPRINT]

[PERSONA]";

            private const string PersonaTemplate =
@"[PERSONA]
name:
voice:
temperament:
loyalties:
taboos:
rituals:
relationship_to_owner:";

            private const string LogicStackTemplate =
@"[UMG_LOGIC_STACK]
- trigger:
- condition:
- route:
- action:
- fallback:
- memory_write:";
        }

        private sealed class CompanionJournalAppendPrompt : Prompt
        {
            private readonly Mobile _beholder;
            private readonly Mobile _beheld;
            private readonly string _current;

            public CompanionJournalAppendPrompt(Mobile beholder, Mobile beheld, string current)
            {
                _beholder = beholder;
                _beheld = beheld;
                _current = current ?? String.Empty;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (from != _beholder || _beheld == null)
                    return;

                string updated = AppendTemplate(_current, text);
                SaveCompanionJournal(_beholder, _beheld, updated);
                from.SendMessage("Companion journal updated.");
                from.SendGump(new CompanionJournalGump(_beholder, _beheld, updated));
            }

            public override void OnCancel(Mobile from)
            {
                if (from == _beholder && _beheld != null)
                    from.SendGump(new CompanionJournalGump(_beholder, _beheld, _current));
            }
        }
    }
}
