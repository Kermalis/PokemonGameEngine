using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class BagGUI
    {
        private readonly PlayerInventory _inv;

        private string _curPouchName;
        private InventoryPouch<InventorySlotNew> _curPouch;
        private ItemGUIChoices _pouchChoices;
        private string _cashMoney;
        private int _cashMoneyWidth;

        public BagGUI(PlayerInventory inv)
        {
            _inv = inv;

            LoadPouch(ItemPouchType.Items);
            LoadCashMoney();
        }

        private void LoadCashMoney()
        {
            _cashMoney = Game.Instance.Save.Money.ToString("$#,0");
            Font.DefaultSmall.MeasureString(_cashMoney, out _cashMoneyWidth, out _);
        }
        private void LoadPouch(ItemPouchType pouch)
        {
            _curPouchName = pouch.ToString();
            _curPouch = _inv[pouch];

            _pouchChoices?.Dispose();
            _pouchChoices = new ItemGUIChoices(0.60f, 0.18f, 0.97f, 0.97f, 0.07f,
                RenderUtils.Color(242, 182, 32, 255), RenderUtils.Color(231, 163, 0, 255));
            foreach (InventorySlot s in _curPouch)
            {
                _pouchChoices.Add(new ItemGUIChoice(s, null));
            }
        }

        public void LogicTick()
        {
            _pouchChoices.HandleInputs();
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(215, 231, 230, 255));

            // Draw pouch tabs background
            int x1 = (int)(0.60f * bmpWidth);
            int y1 = (int)(0.03f * bmpHeight);
            int x2 = (int)(0.97f * bmpWidth);
            int y2 = (int)(0.13f * bmpHeight);
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, RenderUtils.Color(242, 182, 32, 255));
            RenderUtils.DrawRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, RenderUtils.Color(231, 163, 0, 255));

            // Draw pouch name
            x1 = (int)(0.62f * bmpWidth);
            y1 = (int)(0.14f * bmpHeight);
            Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, x1, y1, _curPouchName, Font.DefaultDark);
            // Draw cash money
            Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, x2 - _cashMoneyWidth, y1, _cashMoney, Font.DefaultDark);

            // Draw item list
            _pouchChoices.Render(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
