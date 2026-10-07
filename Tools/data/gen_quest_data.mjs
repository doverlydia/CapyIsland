#!/usr/bin/env node
// Generates Assets/Scripts/Game/QuestData.g.cs from docs/reference/save_data.xml: achievement ids,
// each card's item list, and each quest's trigger, set-up and reward. Ids and numbers ONLY — none of
// the original prose is copied. Every word the player reads is written in
// Assets/Scripts/Game/StoryText.cs.
//
// Usage: node Tools/data/gen_quest_data.mjs

import fs from "node:fs";

const xml = fs.readFileSync("docs/reference/save_data.xml", "utf8");
const tag = (block, name) => {
  const m = block.match(new RegExp(`<${name}>([\\s\\S]*?)</${name}>`));
  return m ? m[1].trim() : "";
};
const all = (block, name) => [...block.matchAll(new RegExp(`<${name}>([\\s\\S]*?)</${name}>`, "g"))].map((m) => m[1].trim());

const esc = (s) => s.replace(/\\/g, "\\\\").replace(/"/g, '\\"').replace(/\r?\n/g, " ").replace(/\s+/g, " ").trim();
const str = (s) => `"${esc(s)}"`;
// Item ids: the capybara and its house stand in for the original's species and its house.
const id = (s) => s === "Bunni House" ? "Capy House" : s === "Bunni" ? "Capybara" : s;

const motes = [...xml.matchAll(/<mote name="([^"]+)">([\s\S]*?)<\/mote>/g)].map(([, name, b]) =>
  `            new MoteDef(${str(name)}, ${+tag(b, "id")}, ${str(tag(b, "func"))}),`);

const cards = [...xml.matchAll(/<card name="([^"]+)">([\s\S]*?)<\/card>/g)].map(([, , b]) =>
  `            new CardDef(${+tag(b, "id")}, new[] { ${all(b, "func").map(id).map(str).join(", ")} }),`);

const quest = (b, giver) =>
  `            new QuestDef(QuestSource.${giver}, ${+tag(b, "id")},\n` +
  `                trigger: ${str(tag(b, "trigger_mote"))}, setUp: ${str(tag(b, "set_up"))}, completeMote: ${str(tag(b, "complete_mote"))},\n` +
  `                rewardItem: ${str(tag(b, "reward_item"))}, wood: ${+tag(b, "wood") || 0}, gold: ${+tag(b, "gold") || 0}, gems: ${+tag(b, "gems") || 0}),`;

const ghosts = [...xml.matchAll(/<ghost>([\s\S]*?)<\/ghost>/g)].map(([, b]) => quest(b, "Ghost"));
const sweetheart = [...xml.matchAll(/<coriander>([\s\S]*?)<\/coriander>/g)].map(([, b]) => quest(b, "Coriander"));
const captain = [...xml.matchAll(/<piratepeach>([\s\S]*?)<\/piratepeach>/g)].map(([, b]) => quest(b, "Peach"));

const out = `// Originally generated from the shipped balance data by Tools/data/gen_quest_data.mjs.
// Ids, triggers and rewards only: every word the player reads lives in StoryText.cs.

namespace CapyIsland.Game
{
    public static partial class QuestData
    {
        public static readonly MoteDef[] Motes =
        {
${motes.join("\n")}
        };

        public static readonly CardDef[] Cards =
        {
${cards.join("\n")}
        };

        public static readonly QuestDef[] Ghosts =
        {
${ghosts.join("\n")}
        };

        /// <summary>Miso's missions, in the order she gives them.</summary>
        public static readonly QuestDef[] Coriander =
        {
${sweetheart.join("\n")}
        };

        /// <summary>Captain Wasabi's missions, in the order she gives them.</summary>
        public static readonly QuestDef[] Peach =
        {
${captain.join("\n")}
        };
    }
}
`;
fs.writeFileSync("Assets/Scripts/Game/QuestData.g.cs", out);
console.log(`QuestData.g.cs: ${motes.length} achievements, ${cards.length} cards, ${ghosts.length} spirit quests, ${sweetheart.length} Miso, ${captain.length} Wasabi`);
