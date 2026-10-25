using System;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Building together. A visitor can always help with chores on a friend's island (plant flower
    /// gardens for hungry workers, drop Shoo Bombs on pests); to put down anything else they need
    /// the host's yes. Either side can raise it: the host is asked when a friend arrives, and a
    /// visitor can ask from the host's face in the corner (VisitorsColumn).
    ///
    /// Islanders the game makes up always say yes when you ask — and when one of them visits
    /// you, you're asked too, as with anyone; they just carry on with chores either way. Strangers
    /// are never asked or allowed: building is for friends.
    ///
    /// With a real friend live, what either of you puts down appears on the other's screen too
    /// (Live), so the host's island really changes.
    /// </summary>
    public static class CoBuild
    {
        /// <summary>On a friend's island: they said you may build.</summary>
        public static bool Allowed { get; private set; }

        /// <summary>At home: you said your visitor may build.</summary>
        public static bool GuestAllowed { get; private set; }

        /// <summary>Waiting for the host's answer.</summary>
        public static bool Asking { get; private set; }

        /// <summary>Allowed, GuestAllowed or Asking changed.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Allowed = GuestAllowed = Asking = false;
            Changed = null;
        }

        /// <summary>A fresh island (a flight, home again): permissions start over.</summary>
        public static void Reset()
        {
            Allowed = GuestAllowed = Asking = false;
            Changed?.Invoke();
        }

        /// <summary>Chore things a visitor may always put down: flower gardens and Shoo Bombs.</summary>
        public static bool IsChore(string storeName) => StoreData.IsChoreItem(storeName);

        /// <summary>May the player put this down here? Null if yes, or why not.</summary>
        public static string WhyCantPlace(string storeName)
        {
            if (!Visit.Active || IsChore(storeName)) return null;
            if (StoreData.StaysHome(storeName)) return "That one stays on your own island. Put it down at home!";
            if (Allowed) return null;
            if (!Visit.HostIsFriend) return UI.Loc.F("This is {0}'s island. Add them as a friend, then you can ask to build together.", Visit.Host.name);
            return UI.Loc.F("Ask {0} if you can build here first: tap their face in the corner.", Visit.Host.name);
        }

        // ---------------------------------------------------------------- the visitor asks

        /// <summary>Can the "Build together" button be offered on this island?</summary>
        public static bool CanAsk => Visit.Active && Visit.HostIsFriend && !Allowed && !Asking;

        public static async void Ask()
        {
            if (!CanAsk) return;
            var host = Visit.Host;
            if (!host.IsReal)
            {
                // An islander always says yes, after a moment's thought.
                Asking = true;
                Changed?.Invoke();
                await System.Threading.Tasks.Task.Delay(1400);
                if (!Visit.Active || Visit.Host != host) return;
                Asking = false;
                Granted(true);
                if (VisitorBot.Current != null) VisitorBot.Current.Emote(Emoji.Heart);
                return;
            }
            if (Live.Now != Live.Mode.Visiting || !Live.Connected)
            {
                Hints.Answer(UI.Loc.F("{0} isn't home. Visit them while they're online to build together.", host.name));
                return;
            }
            Asking = true;
            Changed?.Invoke();
            Live.AskToBuild();
            Hints.Answer(UI.Loc.F("Asking {0}…", host.name));
        }

        /// <summary>The host's answer (from a bot at once, or a real friend over the live link).</summary>
        public static void Granted(bool yes)
        {
            Asking = false;
            Allowed = yes && Visit.Active;
            Changed?.Invoke();
            var host = Visit.Host;
            if (host == null) return;
            if (yes)
            {
                Sfx.Play(SfxKind.Chest, 0.5f);
                Hints.Show(UI.Loc.F("{0} said yes! You can build on their island now.", host.name));
            }
            else Hints.Show(UI.Loc.F("{0} would rather you just helped out for now.", host.name));
        }

        // ---------------------------------------------------------------- the host is asked

        /// <summary>
        /// A friend has arrived, or asked: let them build? A real friend hears the answer; an
        /// islander carries on with chores whatever you say.
        /// </summary>
        public static void AskHost(FriendProfile guest)
        {
            if (guest == null || Visit.Active || !Friends.IsFriend(guest.id)) return;
            Attention.Ask("cobuild:" + guest.id, () =>
                UI.PromptScreen.Ask("Build together?", guest,
                    UI.Loc.F("Let <b>{0}</b> build on your island too? They can always help with chores.", guest.name),
                    "Build together", "Just chores",
                    () => Answer(guest, true), () => Answer(guest, false)),
                () => !Visit.Active && VisitorBot.IsHere(guest.id));
        }

        static void Answer(FriendProfile guest, bool yes)
        {
            GuestAllowed = yes;
            Changed?.Invoke();
            if (guest.IsReal) Live.AnswerBuild(guest, yes);
            if (yes) Hints.Answer(UI.Loc.F("{0} can build on your island now.", guest.name));
        }
    }
}
