using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Player
{
    internal sealed class BagGUI
    {
        private readonly Inventory<InventorySlotNew> _inv;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private bool _isOnParty = false;

        private InventoryPouch<InventorySlotNew> _curPouch;
        private readonly PartyPkmnGUIChoices _partyChoices;
        private ItemGUIChoices _pouchChoices;

        private readonly GUIString _bagText;
        private GUIString _curPouchName;
        private GUIString _cashMoney;
        private int _cashMoneyWidth;

        public BagGUI(Inventory<InventorySlotNew> inv, Party party, Action onClosed)
        {
            _inv = inv;

            _partyChoices = new PartyPkmnGUIChoices(0.03f, 0.18f, 0.47f, 0.97f, 0.004f);
            foreach (PartyPokemon pkmn in party)
            {
                _partyChoices.Add(new PartyPkmnGUIChoice(pkmn, null));
            }

            LoadPouch(ItemPouchType.Items);
            LoadCashMoney();

            _bagText = new GUIString("BAG", Font.Default, FontColors.DefaultDarkGray_I, scale: 2);

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeInBag);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void LoadCashMoney()
        {
            string str = Engine.Instance.Save.Money.ToString("$#,0");
            _cashMoney?.Delete(Game.OpenGL);
            _cashMoney = new GUIString(str, Font.DefaultSmall, FontColors.DefaultDarkGray_I);
            _cashMoneyWidth = (int)Font.DefaultSmall.MeasureString(str).Width;
        }
        private void LoadPouch(ItemPouchType pouch)
        {
            _curPouchName?.Delete(Game.OpenGL);
            _curPouchName = new GUIString(pouch.ToString(), Font.DefaultSmall, FontColors.DefaultDarkGray_I);
            _curPouch = _inv[pouch];

            _pouchChoices?.Dispose();
            _pouchChoices = new ItemGUIChoices(0.60f, 0.18f, 0.97f, 0.97f, 0.07f, ColorF.FromRGB(245, 200, 37), ColorF.FromRGB(231, 163, 0));
            foreach (InventorySlot s in _curPouch)
            {
                _pouchChoices.Add(new ItemGUIChoice(s, null));
            }
        }

        private void CloseMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutBag);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void CB_FadeInBag()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.SetCallback(CB_LogicTick);
                Engine.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private void CB_FadeOutBag()
        {
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _partyChoices.Dispose();
                GL gl = Game.OpenGL;
                _bagText.Delete(gl);
                _curPouchName.Delete(gl);
                _cashMoney.Delete(gl);
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

        private void RCB_Fading(GL gl)
        {
            RCB_RenderTick(gl);
            _fadeTransition.Render(gl);
        }
        private void RCB_RenderTick(GL gl)
        {
            // Background
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(215, 231, 230, 255), Renderer.Color(231, 163, 0, 255), Renderer.Color(242, 182, 32, 255));
            GLHelper.ClearColor(gl, ColorF.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // BAG
            _bagText.Render(gl, Pos2D.FromRelative(0.02f, 0.01f));

            _partyChoices.Render(gl);

            // Draw pouch tabs background
            var rect = new Rect2D(Pos2D.FromRelative(0.60f, 0.03f), Pos2D.FromRelative(0.97f, 0.13f));
            GUIRenderer.Instance.FillRectangle(ColorF.FromRGB(245, 200, 37), rect); // TODO: ROUNDED 10
            GUIRenderer.Instance.DrawRectangle(ColorF.FromRGB(231, 163, 0), rect); // TODO: ROUNDED 10

            // Draw pouch name
            var pos = Pos2D.FromRelative(0.62f, 0.14f);
            _curPouchName.Render(gl, pos);
            // Draw cash money
            pos.X = rect.GetRight() + 1 - _cashMoneyWidth;
            _cashMoney.Render(gl, pos);

            // Draw item list
            _pouchChoices.Render(gl);
        }
    }
}
