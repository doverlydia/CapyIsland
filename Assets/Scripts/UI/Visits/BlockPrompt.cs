using System;
using CapyIsland.Game;

namespace CapyIsland.UI
{
    /// <summary>
    /// "Block them?" — asked before anyone is blocked, from wherever you met them: a friend's row,
    /// a friend request, a face in the corner during a visit, a stranger on your server. Saying yes
    /// blocks them (RealFriends.Block), which also ends any friendship and sends them away if
    /// they're with you now. Undone from the bottom of Neighbours.
    /// </summary>
    public static class BlockPrompt
    {
        /// <summary>Asks, then blocks. <paramref name="face"/> is who to picture (null for none).</summary>
        public static void Ask(string member, string name, FriendProfile face = null, Action done = null)
        {
            if (string.IsNullOrEmpty(member)) return;
            if (RealFriends.IsBlocked(member)) { done?.Invoke(); return; }
            PromptScreen.Ask("Block?", face,
                Loc.F("Block {0}? They won't be able to visit you, message you or send you friend requests. You can unblock them later in Friends.", name),
                "Block", "Cancel", () =>
                {
                    _ = RealFriends.Block(member, name);   // in force here at once; the service hears it after
                    Hints.Answer(Loc.F("{0} is blocked.", name));
                    done?.Invoke();
                }, null, null, IconPainter.Block);
        }

        public static void Ask(FriendProfile who, Action done = null)
        {
            if (who == null || !who.IsReal) return;
            Ask(who.member, who.name, who, done);
        }
    }
}
