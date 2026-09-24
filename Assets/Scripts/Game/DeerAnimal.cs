using UnityEngine;

namespace CapyIsland.Game
{
    /// <summary>
    /// A deer (Animals/DeerItem). Never works and can't be picked up. It roams the whole map eating
    /// flowers — posies, tulips, roses and fallen fruit — and shaking fruit trees, and whatever it
    /// eats is gone for nobody's benefit, a parent garden included. Only a Bomb or a Deer Bomb gets
    /// rid of one.
    ///
    /// Speed 5 for walking and eating (0.1 × 5 / 3 of a flower per bite).
    /// </summary>
    public class DeerAnimal : WildForager, IWild
    {
        const int Speed = 5;

        protected override int WalkSpeed => Speed;

        protected override string ArrivalHint =>
            "A deer is eating all your flowers! Find it and put a bomb under its booty to make it go boom.";

        public void GoWild() => BeginLife();

        protected override void Search()
        {
            // One pass over flowers and fruit trees with fruit: the nearest child flower or tree,
            // else the nearest parent flower.
            var flower = FindNode(n => n.Family == NodeFamily.Flower);
            var tree = FindFruitTreeWithFruit(out float treeDistance);
            if (flower != null && tree != null)
            {
                float flowerDistance = BunniSpace.WorldDistance(transform.position, flower.transform.position);
                bool treeWins = !flower.IsParent ? treeDistance < flowerDistance : true;
                if (treeWins) { flower.Reserved = false; flower = null; }
                else tree = null;
            }
            TargetNode = flower;
            TargetTree = tree;
            if (tree != null) tree.Reserved = true;
        }

        protected override float HitAmount(Harvestable node) => BunniRules.ScalePerHit(Speed);
    }
}
