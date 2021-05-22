using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Player
{
    internal sealed class BagGUI
    {
        private readonly PlayerInventory _inv;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private bool _isOnParty = false;

        private string _curPouchName;
        private InventoryPouch<InventorySlotNew> _curPouch;
        private readonly PartyPkmnGUIChoices _partyChoices;
        private ItemGUIChoices _pouchChoices;
        private string _cashMoney;
        private int _cashMoneyWidth;

        public unsafe BagGUI(PlayerInventory inv, Party party, Action onClosed)
        {
            _inv = inv;

            _partyChoices = new PartyPkmnGUIChoices(0.03f, 0.18f, 0.47f, 0.97f, 0.004f);
            foreach (PartyPokemon pkmn in party)
            {
                _partyChoices.Add(new PartyPkmnGUIChoice(pkmn, null));
            }

            LoadPouch(ItemPouchType.Items);
            LoadCashMoney();

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInBag);
            Game.Instance.SetRCallback(RCB_Fading);
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
                RenderUtils.Color(245, 200, 37, 255), RenderUtils.Color(231, 163, 0, 255));
            foreach (InventorySlot s in _curPouch)
            {
                _pouchChoices.Add(new ItemGUIChoice(s, null));
            }
        }

        private unsafe void CloseMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutBag);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInBag()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutBag()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _onClosed.Invoke();
                _onClosed = null;
            }
        }
        private void CB_LogicTick()
        {
            if (InputManager.IsPressed(Key.B))
            {
                CloseMenu();
                return;
            }
            if (_isOnParty)
            {
                if (InputManager.IsPressed(Key.Right))
                {
                    _isOnParty = false;
                    return;
                }
                _partyChoices.HandleInputs();
            }
            else
            {
                if (InputManager.IsPressed(Key.Left))
                {
                    _isOnParty = true;
                    return;
                }
                _pouchChoices.HandleInputs();
            }
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(215, 231, 230, 255), RenderUtils.Color(231, 163, 0, 255), RenderUtils.Color(242, 182, 32, 255));

            // BAG
            Font.Default.DrawStringScaled(bmpAddress, bmpWidth, bmpHeight, 0.02f, 0.01f, 2, "BAG", Font.DefaultDarkGray_I);

            _partyChoices.Render(bmpAddress, bmpWidth, bmpHeight);

            // Draw pouch tabs background
            int x1 = (int)(0.60f * bmpWidth);
            int y1 = (int)(0.03f * bmpHeight);
            int x2 = (int)(0.97f * bmpWidth);
            int y2 = (int)(0.13f * bmpHeight);
            RenderUtils.FillRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, RenderUtils.Color(245, 200, 37, 255));
            RenderUtils.DrawRoundedRectangle(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, 10, RenderUtils.Color(231, 163, 0, 255));

            // Draw pouch name
            x1 = (int)(0.62f * bmpWidth);
            y1 = (int)(0.14f * bmpHeight);
            Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, x1, y1, _curPouchName, Font.DefaultDarkGray_I);
            // Draw cash money
            Font.DefaultSmall.DrawString(bmpAddress, bmpWidth, bmpHeight, x2 - _cashMoneyWidth, y1, _cashMoney, Font.DefaultDarkGray_I);

            // Draw item list
            _pouchChoices.Render(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
