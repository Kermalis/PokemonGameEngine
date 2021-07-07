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
        private readonly Inventory<InventorySlotNew> _inv;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private bool _isOnParty = false;

        private string _curPouchName;
        private InventoryPouch<InventorySlotNew> _curPouch;
        private readonly PartyPkmnGUIChoices _partyChoices;
        private ItemGUIChoices _pouchChoices;
        private string _cashMoney;
        private int _cashMoneyWidth;

        public unsafe BagGUI(Inventory<InventorySlotNew> inv, Party party, Action onClosed)
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
                Renderer.Color(245, 200, 37, 255), Renderer.Color(231, 163, 0, 255));
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
                _onClosed();
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

        private unsafe void RCB_Fading(uint* dst, int dstW, int dstH)
        {
            RCB_RenderTick(dst, dstW, dstH);
            _fadeTransition.Render(dst, dstW, dstH);
        }
        private unsafe void RCB_RenderTick(uint* dst, int dstW, int dstH)
        {
            // Background
            Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(215, 231, 230, 255), Renderer.Color(231, 163, 0, 255), Renderer.Color(242, 182, 32, 255));

            // BAG
            Font.Default.DrawStringScaled(dst, dstW, dstH, 0.02f, 0.01f, 2, "BAG", Font.DefaultDarkGray_I);

            _partyChoices.Render(dst, dstW, dstH);

            // Draw pouch tabs background
            int x1 = (int)(0.60f * dstW);
            int y1 = (int)(0.03f * dstH);
            int x2 = (int)(0.97f * dstW);
            int y2 = (int)(0.13f * dstH);
            Renderer.FillRoundedRectangle(dst, dstW, dstH, x1, y1, x2, y2, 10, Renderer.Color(245, 200, 37, 255));
            Renderer.DrawRoundedRectangle(dst, dstW, dstH, x1, y1, x2, y2, 10, Renderer.Color(231, 163, 0, 255));

            // Draw pouch name
            x1 = (int)(0.62f * dstW);
            y1 = (int)(0.14f * dstH);
            Font.DefaultSmall.DrawString(dst, dstW, dstH, x1, y1, _curPouchName, Font.DefaultDarkGray_I);
            // Draw cash money
            Font.DefaultSmall.DrawString(dst, dstW, dstH, x2 - _cashMoneyWidth, y1, _cashMoney, Font.DefaultDarkGray_I);

            // Draw item list
            _pouchChoices.Render(dst, dstW, dstH);
        }
    }
}
