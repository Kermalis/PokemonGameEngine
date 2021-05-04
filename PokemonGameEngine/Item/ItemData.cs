using Kermalis.PokemonBattleEngine.Data;

namespace Kermalis.PokemonGameEngine.Item
{
    internal sealed class ItemData
    {
        public static ItemPouchType GetPouchType(PBEItem item)
        {
            if (item == (PBEItem)631 || item == (PBEItem)632)
            {
                return ItemPouchType.KeyItems; // TODO: For now, oval and shiny charm are key items
            }
            return ItemPouchType.Items; // TODO
        }

        public static PBEStat? GetPowerItemStat(PBEItem item)
        {
            switch (item)
            {
                case PBEItem.PowerAnklet: return PBEStat.Speed;
                case PBEItem.PowerBand: return PBEStat.SpDefense;
                case PBEItem.PowerBelt: return PBEStat.Defense;
                case PBEItem.PowerBracer: return PBEStat.Attack;
                case PBEItem.PowerLens: return PBEStat.SpAttack;
                case PBEItem.PowerWeight: return PBEStat.HP;
            }
            return null;
        }
    }
}
