using Microsoft.Xna.Framework;

namespace Mordred.GameObjects.ItemInventory.Items
{
    public class EdibleItem : WorldItem
    {
        public double EdibleWorth { get; private set; }

        public EdibleItem(int id, double edibleWorth , Color foreground, Color background, int glyph, int? amount = null, string[] droppedBy = null) : 
            base(id, foreground, background, glyph, amount, droppedBy)
        {
            EdibleWorth = edibleWorth;
        }

        private EdibleItem(EdibleItem original) : base(original.Id, original.Animation[0].Foreground, original.Animation[0].Background, original.Animation[0].Glyph, original.Amount, original.DroppedBy)
        {
            EdibleWorth = original.EdibleWorth;
        }

        public override WorldItem Clone()
        {
            return new EdibleItem(this);
        }
    }
}
