﻿using SadRogue.Primitives;

namespace Mordred.GameObjects.ItemInventory.Items
{
    public class EdibleItem : WorldItem
    {
        public double EdibleWorth { get; private set; }

        public EdibleItem(int id, string name, double edibleWorth , Color foreground, Color background, int glyph, int? amount = null, string[] droppedBy = null) : 
            base(id, name, foreground, background, glyph, amount, droppedBy)
        {
            EdibleWorth = edibleWorth;
        }

        private EdibleItem(EdibleItem original) : base(original.Id, original.Name, original.Appearance.Foreground, original.Appearance.Background, original.Appearance.Glyph, original.Amount, ConvertDropRatesToString(original.DroppedBy))
        {
            EdibleWorth = original.EdibleWorth;
        }

        public override WorldItem Clone()
        {
            return new EdibleItem(this);
        }
    }
}
