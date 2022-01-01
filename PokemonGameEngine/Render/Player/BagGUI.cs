﻿using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Pkmn;
using Kermalis.PokemonGameEngine.Render.Transitions;
using System;

namespace Kermalis.PokemonGameEngine.Render.Player
{
    internal sealed class BagGUI
    {
        private static readonly Size2D _renderSize = new(480, 270); // 16:9
        private readonly FrameBuffer _frameBuffer;
        private readonly TripleColorBackground _tripleColorBG;

        private readonly Inventory<InventorySlotNew> _inv;

        private ITransition _transition;
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
            _frameBuffer = FrameBuffer.CreateWithColor(_renderSize);
            _frameBuffer.Use();

            _tripleColorBG = new TripleColorBackground();
            _tripleColorBG.SetColors(Colors.FromRGB(215, 230, 230), Colors.FromRGB(230, 165, 0), Colors.FromRGB(245, 180, 30));

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

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInBag);
        }

        private void LoadCashMoney()
        {
            string str = Game.Instance.Save.Money.ToString("$#,0");
            _cashMoney?.Delete();
            _cashMoney = new GUIString(str, Font.DefaultSmall, FontColors.DefaultDarkGray_I);
            _cashMoneyWidth = (int)Font.DefaultSmall.MeasureString(str).Width;
        }
        private void LoadPouch(ItemPouchType pouch)
        {
            _curPouchName?.Delete();
            _curPouchName = new GUIString(pouch.ToString(), Font.DefaultSmall, FontColors.DefaultDarkGray_I);
            _curPouch = _inv[pouch];

            _pouchChoices?.Dispose();
            _pouchChoices = new ItemGUIChoices(0.60f, 0.18f, 0.97f, 0.97f, 0.07f, Colors.V4FromRGB(245, 200, 37), Colors.V4FromRGB(231, 163, 0));
            foreach (InventorySlot s in _curPouch)
            {
                _pouchChoices.Add(new ItemGUIChoice(s, null));
            }
        }

        private void SetExitFadeOutCallback()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutBag);
        }

        private void CB_FadeInBag()
        {
            Render();
            _transition.Render();
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void CB_FadeOutBag()
        {
            Render();
            _transition.Render();
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _tripleColorBG.Delete();
            _partyChoices.Dispose();
            _bagText.Delete();
            _curPouchName.Delete();
            _cashMoney.Delete();
            _frameBuffer.Delete();
            _onClosed();
            _onClosed = null;
        }
        private void CB_HandleInputs()
        {
            HandleInputs();

            Render();
            _frameBuffer.BlitToScreen();
        }

        private void HandleInputs()
        {
            if (InputManager.JustPressed(Key.B))
            {
                SetExitFadeOutCallback();
                return;
            }

            if (_isOnParty)
            {
                if (InputManager.JustPressed(Key.Right))
                {
                    _isOnParty = false;
                    return;
                }
                _partyChoices.HandleInputs();
            }
            else
            {
                if (InputManager.JustPressed(Key.Left))
                {
                    _isOnParty = true;
                    return;
                }
                _pouchChoices.HandleInputs();
            }
        }

        private void Render()
        {
            // Background
            _tripleColorBG.Render();

            // BAG
            _bagText.Render(Pos2D.FromRelative(0.02f, 0.01f, _renderSize));

            _partyChoices.Render();

            // Draw pouch tabs background
            var rect = new Rect2D(Pos2D.FromRelative(0.60f, 0.03f, _renderSize), Pos2D.FromRelative(0.97f, 0.13f, _renderSize));
            GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(245, 200, 37), rect); // TODO: ROUNDED 10
            GUIRenderer.Instance.DrawRectangle(Colors.V4FromRGB(231, 163, 0), rect); // TODO: ROUNDED 10

            // Draw pouch name
            var pos = Pos2D.FromRelative(0.62f, 0.14f, _renderSize);
            _curPouchName.Render(pos);
            // Draw cash money
            pos.X = rect.GetRight() + 1 - _cashMoneyWidth;
            _cashMoney.Render(pos);

            // Draw item list
            _pouchChoices.Render();
        }
    }
}
