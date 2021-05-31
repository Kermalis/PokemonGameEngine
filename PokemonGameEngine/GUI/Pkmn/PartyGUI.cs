using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUI
    {
        public enum Mode : byte
        {
            PkmnMenu,
            SelectDaycare,
            BattleSwitchIn,
            BattleReplace
        }
        private sealed class GamePartyData
        {
            public readonly Party Party;

            public GamePartyData(Party party, List<PartyGUIMember> members, List<Sprite> sprites)
            {
                Party = party;
                foreach (PartyPokemon pkmn in party)
                {
                    members.Add(new PartyGUIMember(pkmn, sprites));
                }
            }
        }
        private sealed class BattlePartyData
        {
            public readonly SpritedBattlePokemonParty Party;

            public BattlePartyData(SpritedBattlePokemonParty party, List<PartyGUIMember> members, List<Sprite> sprites)
            {
                Party = party;
                foreach (PBEBattlePokemon pkmn in party.BattleParty)
                {
                    SpritedBattlePokemon sPkmn = party[pkmn]; // Use battle party's order
                    members.Add(new PartyGUIMember(sPkmn, sprites));
                }
            }
        }

        private readonly Mode _mode;
        private readonly bool _allowBack;
        private readonly bool _useGamePartyData;
        private readonly GamePartyData _gameParty;
        private readonly BattlePartyData _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly List<Sprite> _sprites;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;
        private int _selectionForSummary;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private string _message;

        private int _selectionX;
        private int _selectionY;

        #region Open & Close GUI

        public unsafe PartyGUI(Party party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = true;
            _useGamePartyData = true;
            _sprites = new List<Sprite>();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _gameParty = new GamePartyData(party, _members, _sprites);
            _members[0].SetBigBounce();

            if (mode == Mode.SelectDaycare)
            {
                SetSelectionVar(-1);
            }

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe PartyGUI(SpritedBattlePokemonParty party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = mode != Mode.BattleReplace; // Disallow back for BattleReplace
            _useGamePartyData = false;
            _sprites = new List<Sprite>();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _battleParty = new BattlePartyData(party, _members, _sprites);
            _members[0].SetBigBounce();

            if (mode == Mode.BattleSwitchIn)
            {
                SetSelectionVar(-1);
            }
            else if (mode == Mode.BattleReplace)
            {
                SetBattleReplacementMessage();
            }

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ClosePartyMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        private unsafe void OnSummaryClosed()
        {
            _textChoicesWindow.IsInvisible = false;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInThenGoToChoicesCB);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _onClosed.Invoke();
                _onClosed = null;
            }
        }
        private unsafe void CB_FadeInThenGoToChoicesCB()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_Choices);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutToSummary()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _textChoicesWindow.IsInvisible = true;
                if (_useGamePartyData)
                {
                    _ = new SummaryGUI(_gameParty.Party[_selectionForSummary], SummaryGUI.Mode.JustView, OnSummaryClosed);
                }
                else
                {
                    SpritedBattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon bPkmn = party.BattleParty[_selectionForSummary];
                    SpritedBattlePokemon sPkmn = party[bPkmn];
                    _ = new SummaryGUI(sPkmn, SummaryGUI.Mode.JustView, OnSummaryClosed);
                }
            }
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }

        #endregion

        private int GetPartySize()
        {
            return _useGamePartyData ? _gameParty.Party.Count : _battleParty.Party.SpritedParty.Length;
        }
        private int SelectionCoordsToPartyIndex(int col, int row)
        {
            if (row == -1)
            {
                return -1;
            }
            int i = row * 2 + col;
            if (i >= GetPartySize())
            {
                return -1;
            }
            return i;
        }
        private void UpdateBounces(int oldCol, int oldRow)
        {
            int i = SelectionCoordsToPartyIndex(oldCol, oldRow);
            if (i != -1)
            {
                _members[i].SetSmallBounce();
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != -1)
            {
                _members[i].SetBigBounce();
            }
        }
        private static void SetSelectionVar(short index)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = index;
        }

        #region Battle replacement

        private void SetBattleReplacementMessage()
        {
            _message = string.Format("You must send in {0} Pokémon.", BattleGUI.Instance.SwitchesRequired);
        }
        private static bool CanUsePositionForBattle(PBEFieldPosition pos)
        {
            return !BattleGUI.Instance.PositionStandBy.Contains(pos) && BattleGUI.Instance.Trainer.OwnsSpot(pos) && BattleGUI.Instance.Trainer.Team.TryGetPokemon(pos) == null;
        }
        private void SelectForBattleReplacement(PBEBattlePokemon pkmn, PBEFieldPosition pos)
        {
            CloseChoices();
            BattleGUI.Instance.Switches.Add(new PBESwitchIn(pkmn, pos));
            BattleGUI.Instance.StandBy.Add(pkmn);
            BattleGUI.Instance.PositionStandBy.Add(pos);
            if (--BattleGUI.Instance.SwitchesRequired == 0)
            {
                _message = null;
                ClosePartyMenu();
                return;
            }

            SetBattleReplacementMessage();
            // Update standby color of the one we chose
            for (int i = 0; i < _members.Count; i++)
            {
                if (_battleParty.Party.BattleParty[i] == pkmn)
                {
                    _members[i].UpdateColorAndRedraw();
                    break;
                }
            }
            Game.Instance.SetCallback(CB_LogicTick);
        }
        private void SetUpPositionQuery(int index, bool left, bool center, bool right)
        {
            SpritedBattlePokemonParty party = _battleParty.Party;
            PBEBattlePokemon bPkmn = party.BattleParty[index];
            SpritedBattlePokemon sPkmn = party[bPkmn];
            PartyPokemon pkmn = sPkmn.PartyPkmn;
            _message = string.Format("Send {0} where?", pkmn.Nickname);
            CloseChoices();

            void BackCommand()
            {
                CloseChoices();
                BringUpPkmnActions(index);
            }

            _textChoices = new TextGUIChoices(0, 0, backCommand: BackCommand, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            if (left)
            {
                _textChoices.Add(new TextGUIChoice("Send Left", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Left)));
            }
            if (center)
            {
                _textChoices.Add(new TextGUIChoice("Send Center", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Center)));
            }
            if (right)
            {
                _textChoices.Add(new TextGUIChoice("Send Right", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Right)));
            }
            _textChoices.Add(new TextGUIChoice("Cancel", BackCommand));
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            Game.Instance.SetCallback(CB_Choices);
        }

        #endregion

        #region Field Moves

        private void AddFieldMovesToActions(PartyPokemon pkmn, int index)
        {
            void Add(PBEMove move, Action command)
            {
                string str = PBELocalizedString.GetMoveName(move).English;
                _textChoices.Add(new TextGUIChoice(str, command, fontColors: Font.DefaultBlue_I));
            }

            Moveset moves = pkmn.Moveset;
            if (moves.Contains(PBEMove.Surf))
            {
                Add(PBEMove.Surf, () => Action_FieldSurf(index));
            }
        }
        private void SetCantUseThatHere()
        {
            _message = "Can't use that here.";
            Game.Instance.SetCallback(CB_CantUseFieldMove);
        }

        private void Action_FieldSurf(int index)
        {
            if (!PlayerObj.Player.CanUseSurfFromCurrentPosition())
            {
                SetCantUseThatHere();
                return;
            }
            SetSelectionVar((short)index);
            _onClosed = OverworldGUI.Instance.ReturnToFieldAndUseSurf;
            CloseChoices();
            ClosePartyMenu();
        }

        #endregion

        private void Action_SelectPartyPkmn(int index)
        {
            switch (_mode)
            {
                case Mode.SelectDaycare:
                case Mode.BattleSwitchIn:
                {
                    SetSelectionVar((short)index);
                    CloseChoices();
                    ClosePartyMenu();
                    return;
                }
                case Mode.BattleReplace:
                {
                    SpritedBattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon bPkmn = party.BattleParty[index];
                    switch (BattleGUI.Instance.Battle.BattleFormat)
                    {
                        case PBEBattleFormat.Single:
                        {
                            SelectForBattleReplacement(bPkmn, PBEFieldPosition.Center);
                            return;
                        }
                        case PBEBattleFormat.Double:
                        {
                            bool left = CanUsePositionForBattle(PBEFieldPosition.Left);
                            bool right = CanUsePositionForBattle(PBEFieldPosition.Right);
                            if (left && !right)
                            {
                                SelectForBattleReplacement(bPkmn, PBEFieldPosition.Left);
                                return;
                            }
                            if (!left && right)
                            {
                                SelectForBattleReplacement(bPkmn, PBEFieldPosition.Right);
                                return;
                            }
                            SetUpPositionQuery(index, true, false, true);
                            return;
                        }
                        case PBEBattleFormat.Triple:
                        case PBEBattleFormat.Rotation:
                        {
                            bool left = CanUsePositionForBattle(PBEFieldPosition.Left);
                            bool center = CanUsePositionForBattle(PBEFieldPosition.Center);
                            bool right = CanUsePositionForBattle(PBEFieldPosition.Right);
                            if (left && !center && !right)
                            {
                                SelectForBattleReplacement(bPkmn, PBEFieldPosition.Left);
                                return;
                            }
                            if (!left && center && !right)
                            {
                                SelectForBattleReplacement(bPkmn, PBEFieldPosition.Center);
                                return;
                            }
                            if (!left && !center && right)
                            {
                                SelectForBattleReplacement(bPkmn, PBEFieldPosition.Right);
                                return;
                            }
                            SetUpPositionQuery(index, left, center, right);
                            return;
                        }
                        default: throw new Exception();
                    }
                }
                default: throw new Exception();
            }
        }
        private unsafe void Action_BringUpSummary(int index)
        {
            _selectionForSummary = index;
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutToSummary);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private void BringUpPkmnActions(int index)
        {
            string nickname;
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToLogicTick, font: Font.Default, fontColors: Font.DefaultDarkGray_I, selectedColors: Font.DefaultYellow_O);
            switch (_mode)
            {
                case Mode.PkmnMenu:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    _textChoices.Add(new TextGUIChoice("Check summary", () => Action_BringUpSummary(index)));
                    AddFieldMovesToActions(pkmn, index);
                    break;
                }
                case Mode.SelectDaycare:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    if (!pkmn.IsEgg)
                    {
                        _textChoices.Add(new TextGUIChoice("Select", () => Action_SelectPartyPkmn(index)));
                    }
                    _textChoices.Add(new TextGUIChoice("Check summary", () => Action_BringUpSummary(index)));
                    break;
                }
                case Mode.BattleSwitchIn:
                case Mode.BattleReplace: // Currently same logic
                {
                    SpritedBattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon bPkmn = party.BattleParty[index];
                    SpritedBattlePokemon sPkmn = party[bPkmn];
                    PartyPokemon pkmn = sPkmn.PartyPkmn;
                    nickname = pkmn.Nickname;
                    // Cannot switch in if active already or fainted, or in the switch stand by
                    if (!pkmn.IsEgg && bPkmn.FieldPosition == PBEFieldPosition.None && bPkmn.HP > 0 && !BattleGUI.Instance.StandBy.Contains(bPkmn))
                    {
                        _textChoices.Add(new TextGUIChoice("Switch In", () => Action_SelectPartyPkmn(index)));
                    }
                    _textChoices.Add(new TextGUIChoice("Check summary", () => Action_BringUpSummary(index)));
                    break;
                }
                default: throw new Exception();
            }

            _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesThenGoToLogicTick));
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            _message = string.Format("Do what with {0}?", nickname);
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesThenGoToLogicTick()
        {
            CloseChoices();
            if (_mode == Mode.BattleReplace)
            {
                SetBattleReplacementMessage();
            }
            else
            {
                _message = null;
            }
            Game.Instance.SetCallback(CB_LogicTick);
        }
        private unsafe void RenderChoicesOntoWindow()
        {
            _textChoicesWindow.ClearImage();
            Image i = _textChoicesWindow.Image;
            fixed (uint* bmpAddress = i.Bitmap)
            {
                _textChoices.Render(bmpAddress, i.Width, i.Height);
            }
        }

        private void CB_Choices()
        {
            Sprite.DoCallbacks(_sprites);
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            if (_textChoicesWindow is null)
            {
                return; // Was just closed
            }
            if (s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow();
            }
        }
        private void CB_CantUseFieldMove()
        {
            if (!InputManager.IsPressed(Key.A) && !InputManager.IsPressed(Key.B))
            {
                return;
            }

            int index = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            // Assume Mode.PkmnMenu
            _message = string.Format("Do what with {0}?", _gameParty.Party[index].Nickname);
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CB_LogicTick()
        {
            Sprite.DoCallbacks(_sprites);
            if (InputManager.IsPressed(Key.A))
            {
                if (_selectionY == -1)
                {
                    if (_allowBack)
                    {
                        ClosePartyMenu();
                    }
                }
                else
                {
                    int i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                    BringUpPkmnActions(i);
                }
                return;
            }
            if (_allowBack && InputManager.IsPressed(Key.B))
            {
                ClosePartyMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                if (_selectionX == 1)
                {
                    _selectionX = 0;
                    UpdateBounces(1, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                if (_selectionX == 0 && SelectionCoordsToPartyIndex(1, _selectionY) != -1)
                {
                    _selectionX = 1;
                    UpdateBounces(0, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                int oldY = _selectionY;
                if (oldY != -1)
                {
                    if (SelectionCoordsToPartyIndex(_selectionX, oldY + 1) == -1)
                    {
                        if (!_allowBack)
                        {
                            return;
                        }
                        _selectionY = -1;
                    }
                    else
                    {
                        _selectionY++;
                    }
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Up))
            {
                int oldY = _selectionY;
                if (oldY == -1)
                {
                    _selectionY = (GetPartySize() - 1) / 2;
                    UpdateBounces(_selectionX, oldY);
                }
                else if (oldY > 0)
                {
                    _selectionY--;
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
        }

        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(222, 50, 60, 255), RenderUtils.Color(190, 40, 50, 255), RenderUtils.Color(255, 180, 200, 255));

            for (int i = 0; i < _members.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = col == 0 ? bmpWidth / 40 : (bmpWidth / 2) + (bmpWidth / 40);
                int y = row * (bmpHeight / 4) + (bmpHeight / 20);
                _members[i].Render(bmpAddress, bmpWidth, bmpHeight, x, y, col == _selectionX && row == _selectionY);
            }

            // Back button
            if (_allowBack)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, 0.5f, 0.2f, _selectionY == -1 ? RenderUtils.Color(96, 48, 48, 255) : RenderUtils.Color(48, 48, 48, 255));
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, "Back", Font.DefaultWhite_I);
            }

            if (_message != null)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, 0.5f, 0.2f, RenderUtils.Color(200, 200, 200, 255));
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, _message, Font.DefaultDarkGray_I);
            }

            Game.Instance.RenderWindows(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
