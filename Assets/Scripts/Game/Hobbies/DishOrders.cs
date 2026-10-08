using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// Once you have a stove, the islanders get peckish. Miso, Captain Wasabi and Salty Kombu each
    /// take a fancy to a dish now and then — it shows in a little bubble over their head. Cook it,
    /// walk over and tap the bubble to hand it over, and they pay you for it (each in their own
    /// way). A few minutes later they will fancy something else.
    /// </summary>
    public class DishOrders : MonoBehaviour
    {
        const string Key = "CapyIsland.DishOrders";

        [Serializable] class Order { public string who; public string dish; public long nextTicks; }
        [Serializable] class Data { public List<Order> orders = new(); }

        Data _data;
        readonly Dictionary<string, WorldBubble> _bubbles = new();
        float _next;

        /// <summary>Someone who can order: an id for the save, their name, and where they stand.</summary>
        struct Customer { public string Id, Name; public Transform At; public SpriteRenderer Body; }

        void Awake() => Load();

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 1.5f;
            bool open = !Visit.Active && CookingStove.Placed() != null && !UI.Cutscene.Playing;
            var here = new HashSet<string>();
            if (open)
            {
                foreach (var c in Customers())
                {
                    here.Add(c.Id);
                    var order = Get(c.Id);
                    if (string.IsNullOrEmpty(order.dish) && DateTime.Now.Ticks >= order.nextTicks)
                    {
                        order.dish = Pick();
                        Save();
                    }
                    if (string.IsNullOrEmpty(order.dish)) continue;
                    if (_bubbles.TryGetValue(c.Id, out var b) && b != null) continue;
                    var customer = c;
                    float top = (c.Body != null ? c.Body.bounds.max.y : c.At.position.y + 1.5f) - c.At.position.y;
                    _bubbles[c.Id] = WorldBubble.Show(c.At, HobbyData.ItemArt("dish:" + order.dish), top + 0.45f, () => Serve(customer));
                }
            }
            // Bubbles for people who are not around (or no stove yet) go.
            var gone = new List<string>();
            foreach (var kv in _bubbles) if (!here.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone)
            {
                if (_bubbles[id] != null) Destroy(_bubbles[id].gameObject);
                _bubbles.Remove(id);
            }
        }

        static IEnumerable<Customer> Customers()
        {
            foreach (var q in FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
            {
                if (q.GiverKind == QuestGiver.Kind.Ghost) continue;
                var body = q.transform.Find("Sprite");
                yield return new Customer
                {
                    Id = q.GiverKind == QuestGiver.Kind.Coriander ? "miso" : "captain",
                    Name = q.Speaker, At = q.transform,
                    Body = body != null ? body.GetComponent<SpriteRenderer>() : null,
                };
            }
            foreach (var pete in FindObjectsByType<PiratePete>(FindObjectsSortMode.None))
            {
                if (!pete.InWorld) continue;
                var body = pete.transform.Find("Sprite");
                yield return new Customer { Id = "kombu", Name = StoryText.Sailor, At = pete.transform, Body = body != null ? body.GetComponent<SpriteRenderer>() : null };
            }
        }

        /// <summary>Something you can cook at your level (more interesting dishes as you improve).</summary>
        static string Pick()
        {
            int level = HobbyLog.Level(Hobby.Cooking);
            var known = new List<DishKind>();
            foreach (var d in HobbyData.Dishes) if (d.MinLevel <= level) known.Add(d);
            return known.Count > 0 ? known[UnityEngine.Random.Range(0, known.Count)].Id : null;
        }

        void Serve(Customer c)
        {
            var order = Get(c.Id);
            var dish = HobbyData.Dish(order.dish);
            if (dish == null) return;
            if (!HobbyLog.Take("dish:" + dish.Id))
            {
                Hints.Show($"{c.Name} would love some {dish.Name}! Cook it at your stove: {Recipe(dish)}.");
                return;
            }

            // Thank you! Each pays in their own way.
            int value = 25 + dish.MinLevel * 20;
            var at = (Vector2)c.At.position + new Vector2(0.9f, -0.5f);
            var parent = ItemCatalog.Instance != null ? ItemCatalog.Instance.ItemParent : null;
            string line;
            switch (c.Id)
            {
                case "captain":
                    TreasureChest.SpawnValue(at, ResourceKind.Gem, 1 + dish.MinLevel / 2, parent);
                    line = $"\"{dish.Name}! Now THAT'S proper ship's cooking!\" The Captain pays you in gems.";
                    break;
                case "kombu":
                    TreasureChest.SpawnValue(at, ResourceKind.Wood, value, parent);
                    TreasureChest.SpawnValue(at + new Vector2(0.8f, 0f), ResourceKind.Gold, value / 2, parent);
                    line = $"\"Arr, {dish.Name}. Reminds me of me old mum's.\" Salty Kombu pays you in wood and gold.";
                    break;
                default:
                    TreasureChest.SpawnValue(at, ResourceKind.Gold, value, parent);
                    line = $"\"{dish.Name}? You remembered!\" Miso beams and pays you in gold.";
                    break;
            }
            HobbyLog.AddXp(Hobby.Cooking, 8 + dish.MinLevel * 4);
            Motes.Trigger("hobby_cook");
            VisitorBot.ShowEmoji(c.Body != null ? c.Body.transform : c.At, Emoji.Heart);
            Sfx.Play(SfxKind.Chest, 0.7f);
            Hints.Show(line);

            order.dish = null;
            order.nextTicks = DateTime.Now.AddMinutes(UnityEngine.Random.Range(4f, 9f)).Ticks;
            Save();
            if (_bubbles.TryGetValue(c.Id, out var b) && b != null) b.Pop();
            _bubbles.Remove(c.Id);
        }

        static string Recipe(DishKind dish)
        {
            var parts = new List<string>();
            foreach (var (item, count) in dish.Needs) parts.Add(count + " " + HobbyData.ItemName(item));
            return string.Join(", ", parts);
        }

        // ---------------------------------------------------------------- saving

        Order Get(string who)
        {
            var o = _data.orders.Find(x => x.who == who);
            if (o != null) return o;
            o = new Order { who = who, nextTicks = DateTime.Now.AddSeconds(20).Ticks };
            _data.orders.Add(o);
            return o;
        }

        void Load()
        {
            try { _data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")) ?? new Data(); }
            catch { _data = new Data(); }
        }

        void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }
    }
}
