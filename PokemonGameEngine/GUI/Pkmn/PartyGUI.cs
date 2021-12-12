using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
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

            public GamePartyData(Party party, List<PartyGUIMember> members, SpriteList sprites)
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

            public BattlePartyData(SpritedBattlePokemonParty party, List<PartyGUIMember> members, SpriteList sprites)
            {
                Party = party;
                foreach (PBEBattlePokemon pkmn in party.BattleParty)
                {
                    SpritedBattlePokemon sPkmn = party[pkmn]; // Use battle party's order
                    members.Add(new PartyGUIMember(sPkmn, sprites));
                }
            }
        }

        /// <summary>The return value of selection modes, also the y index of the cancel/back button</summary>
        public const short NO_PKMN_CHOSEN = -1;

        private readonly Mode _mode;
        private readonly bool _allowBack;
        private readonly bool _useGamePartyData;
        private readonly GamePartyData _gameParty;
        private readonly BattlePartyData _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly SpriteList _sprites;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;
        private int _selectionForSummary;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private GUIString _message;

        private int _selectionX;
        private int _selectionY;

        private GUIString _backStr;

        #region Open & Close GUI

        public PartyGUI(Party party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = true;
            _useGamePartyData = true;
            _sprites = new();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _gameParty = new GamePartyData(party, _members, _sprites);

            if (mode == Mode.SelectDaycare)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }

            FinishConstructor(onClosed);
        }
        public PartyGUI(SpritedBattlePokemonParty party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = mode != Mode.BattleReplace; // Disallow back for BattleReplace
            _useGamePartyData = false;
            _sprites = new();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _battleParty = new BattlePartyData(party, _members, _sprites);

            if (mode == Mode.BattleSwitchIn)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }
            else if (mode == Mode.BattleReplace)
            {
                SetBattleReplacementMessage();
            }

            FinishConstructor(onClosed);
        }
        private void FinishConstructor(Action onClosed)
        {
            _members[0].SetBounce(true);
            _backStr = new GUIString("Back", Font.Default, FontColors.DefaultWhite_I);

            _onClosed = onClosed;

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInParty);
        }

        private void ClosePartyMenu()
        {
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutParty);
        }
        private void OnSummaryClosed()
        {
            _textChoicesWindow.IsInvisible = false;
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInThenGoToChoicesCB);
        }

        private void CB_FadeInParty()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void CB_FadeOutParty()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            foreach (PartyGUIMember m in _members)
            {
                m.Delete();
            }
            _backStr.Delete();
            DeleteMessage();
            // Choices should be closed so no need to dispose
            _onClosed();
            _onClosed = null;
        }
        private void CB_FadeInThenGoToChoicesCB()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CB_FadeOutToSummary()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

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

        private void RenderFading()
        {
            Render();
            _fadeTransition.Render();
        }

        #endregion

        private int GetPartySize()
        {
            return _useGamePartyData ? _gameParty.Party.Count : _battleParty.Party.SpritedParty.Length;
        }
        private int SelectionCoordsToPartyIndex(int col, int row)
        {
            if (row == NO_PKMN_CHOSEN)
            {
                return NO_PKMN_CHOSEN;
            }
            int i = row * 2 + col;
            if (i >= GetPartySize())
            {
                return NO_PKMN_CHOSEN;
            }
            return i;
        }
        private void UpdateBounces(int oldCol, int oldRow)
        {
            int i = SelectionCoordsToPartyIndex(oldCol, oldRow);
            if (i != NO_PKMN_CHOSEN)
            {
                _members[i].SetBounce(false);
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != NO_PKMN_CHOSEN)
            {
                _members[i].SetBounce(true);
            }
        }
        private static void SetSelectionVar(short index)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = index;
        }
        private void SetMessage(string message)
        {
            _message = new GUIString(message, Font.Default, FontColors.DefaultDarkGray_I);
        }
        private void DeleteMessage()
        {
            _message?.Delete();
            _message = null;
        }

        #region Battle replacement

        private void SetBattleReplacementMessage()
        {
            SetMessage(string.Format("You must send in {0} Pokémon.", BattleGUI.Instance.SwitchesRequired));
        }
        private static bool CanUsePositionForBattle(PBEFieldPosition pos)
        {
            return !BattleGUI.Instance.PositionStandBy.Contains(pos) && BattleGUI.Instance.Trainer.OwnsSpot(pos) && !BattleGUI.Instance.Trainer.Team.IsSpotOccupied(pos);
        }
        private void SelectForBattleReplacement(PBEBattlePokemon pkmn, PBEFieldPosition pos)
        {
            CloseChoices();
            BattleGUI.Instance.Switches.Add(new PBESwitchIn(pkmn, pos));
            BattleGUI.Instance.StandBy.Add(pkmn);
            BattleGUI.Instance.PositionStandBy.Add(pos);
            if (--BattleGUI.Instance.SwitchesRequired == 0)
            {
                DeleteMessage();
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
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void SetUpPositionQuery(int index, bool left, bool center, bool right)
        {
            SpritedBattlePokemonParty party = _battleParty.Party;
            PBEBattlePokemon bPkmn = party.BattleParty[index];
            SpritedBattlePokemon sPkmn = party[bPkmn];
            PartyPokemon pkmn = sPkmn.PartyPkmn;
            SetMessage(string.Format("Send {0} where?", pkmn.Nickname));
            CloseChoices();

            void BackCommand()
            {
                CloseChoices();
                BringUpPkmnActions(index);
            }

            _textChoices = new TextGUIChoices(0, 0, backCommand: BackCommand, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            if (left)
            {
                _textChoices.AddOne("Send Left", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Left));
            }
            if (center)
            {
                _textChoices.AddOne("Send Center", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Center));
            }
            if (right)
            {
                _textChoices.AddOne("Send Right", () => SelectForBattleReplacement(bPkmn, PBEFieldPosition.Right));
            }
            _textChoices.AddOne("Cancel", BackCommand);
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(new RelPos2D(0.6f, 0.3f), s, Colors.White4);
            RenderChoicesOntoWindow();
            Game.Instance.SetCallback(CB_Choices);
        }

        #endregion

        #region Field Moves

        private void AddFieldMovesToActions(PartyPokemon pkmn, int index)
        {
            void Add(PBEMove move, Action command)
            {
                string str = PBEDataProvider.Instance.GetMoveName(move).English;
                _textChoices.AddOne(str, command, textColors: FontColors.DefaultBlue_I);
            }

            Moveset moves = pkmn.Moveset;
            if (moves.Contains(PBEMove.Surf))
            {
                Add(PBEMove.Surf, () => Action_FieldSurf(index));
            }
        }
        private void SetCantUseThatHere()
        {
            SetMessage("Can't use that here.");
            Game.Instance.SetCallback(CB_CantUseFieldMove);
        }

        private void Action_FieldSurf(int index)
        {
            if (!PlayerObj.Instance.CanUseSurfFromCurrentPosition())
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
        private void Action_BringUpSummary(int index)
        {
            _selectionForSummary = index;
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToSummary);
        }

        private void BringUpPkmnActions(int index)
        {
            string nickname;
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToHandleInputs, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            switch (_mode)
            {
                case Mode.PkmnMenu:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    AddFieldMovesToActions(pkmn, index);
                    break;
                }
                case Mode.SelectDaycare:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    if (!pkmn.IsEgg)
                    {
                        _textChoices.AddOne("Select", () => Action_SelectPartyPkmn(index));
                    }
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
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
                        _textChoices.AddOne("Switch In", () => Action_SelectPartyPkmn(index));
                    }
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    break;
                }
                default: throw new Exception();
            }

            _textChoices.AddOne("Cancel", CloseChoicesThenGoToHandleInputs);
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(new RelPos2D(0.6f, 0.3f), s, Colors.White4);
            RenderChoicesOntoWindow();
            SetMessage(string.Format("Do what with {0}?", nickname));
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesThenGoToHandleInputs()
        {
            CloseChoices();
            if (_mode == Mode.BattleReplace)
            {
                SetBattleReplacementMessage();
            }
            else
            {
                DeleteMessage();
            }
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void RenderChoicesOntoWindow()
        {
            _textChoices.RenderChoicesOntoWindow(_textChoicesWindow);
        }

        private void CB_Choices()
        {
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            // Check if the window was just closed
            if (_textChoicesWindow is not null && s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow(); // Update selection if it has changed
            }

            Render();
        }
        private void CB_CantUseFieldMove()
        {
            // Wait for input to advance the message
            if (InputManager.IsPressed(Key.A) || InputManager.IsPressed(Key.B))
            {
                int index = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                // Assume Mode.PkmnMenu
                SetMessage(string.Format("Do what with {0}?", _gameParty.Party[index].Nickname));
                Game.Instance.SetCallback(CB_Choices);
            }
            Render();
        }
        private void CB_HandleInputs()
        {
            HandleInputs();
            Render();
        }

        private void HandleInputs()
        {
            if (InputManager.IsPressed(Key.A))
            {
                if (_selectionY == NO_PKMN_CHOSEN)
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
                if (_selectionX == 0 && SelectionCoordsToPartyIndex(1, _selectionY) != NO_PKMN_CHOSEN)
                {
                    _selectionX = 1;
                    UpdateBounces(0, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                int oldY = _selectionY;
                if (oldY != NO_PKMN_CHOSEN)
                {
                    if (SelectionCoordsToPartyIndex(_selectionX, oldY + 1) == NO_PKMN_CHOSEN)
                    {
                        if (!_allowBack)
                        {
                            return;
                        }
                        _selectionY = NO_PKMN_CHOSEN;
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
                if (oldY == NO_PKMN_CHOSEN)
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

        private void Render()
        {
            _sprites.DoCallbacks();

            GL gl = Display.OpenGL;
            // Background
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(222, 50, 60, 255), Renderer.Color(190, 40, 50, 255), Renderer.Color(255, 180, 200, 255));
            gl.ClearColor(Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Size2D dstSize = FrameBuffer.Current.Size;
            for (uint i = 0; i < _members.Count; i++)
            {
                uint col = i % 2;
                uint row = i / 2;
                uint x = col == 0 ? dstSize.Width / 40 : (dstSize.Width / 2) + (dstSize.Width / 40);
                uint y = row * (dstSize.Height / 4) + (dstSize.Height / 20);
                _members[(int)i].Render(new Pos2D((int)x, (int)y), col == _selectionX && row == _selectionY);
            }

            // Back button
            if (_allowBack)
            {
                GUIRenderer.Instance.FillRectangle(_selectionY == NO_PKMN_CHOSEN ? Colors.V4FromRGB(96, 48, 48) : Colors.V4FromRGB(48, 48, 48), new Rect2D(Pos2D.FromRelative(0.5f, 0.8f), Size2D.FromRelative(0.5f, 0.2f)));
                _backStr.Render(Pos2D.FromRelative(0.5f, 0.8f));
            }

            if (_message is not null)
            {
                GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(200, 200, 200), new Rect2D(Pos2D.FromRelative(0f, 0.8f), Size2D.FromRelative(0.5f, 0.2f)));
                _message.Render(Pos2D.FromRelative(0f, 0.8f));
            }

            Window.RenderAll();
        }
    }
}
