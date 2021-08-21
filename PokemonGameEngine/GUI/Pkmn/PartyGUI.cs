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
                SetSelectionVar(-1);
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
                SetSelectionVar(-1);
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
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeInParty);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void ClosePartyMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutParty);
            Engine.Instance.SetRCallback(RCB_Fading);
        }
        private void OnSummaryClosed()
        {
            _textChoicesWindow.IsInvisible = false;
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeInThenGoToChoicesCB);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void CB_FadeInParty()
        {
            _sprites.DoCallbacks();
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.SetCallback(CB_LogicTick);
                Engine.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private void CB_FadeOutParty()
        {
            _sprites.DoCallbacks();
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                GL gl = Game.OpenGL;
                foreach (PartyGUIMember m in _members)
                {
                    m.Delete(gl);
                }
                _backStr.Delete(gl);
                DeleteMessage(gl);
                // Choices should be closed so no need to dispose
                _onClosed();
                _onClosed = null;
            }
        }
        private void CB_FadeInThenGoToChoicesCB()
        {
            _sprites.DoCallbacks();
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Engine.Instance.SetCallback(CB_Choices);
                Engine.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private void CB_FadeOutToSummary()
        {
            _sprites.DoCallbacks();
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

        private void RCB_Fading(GL gl)
        {
            RCB_RenderTick(gl);
            _fadeTransition.Render(gl);
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
                _members[i].SetBounce(false);
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != -1)
            {
                _members[i].SetBounce(true);
            }
        }
        private static void SetSelectionVar(short index)
        {
            Engine.Instance.Save.Vars[Var.SpecialVar_Result] = index;
        }
        private void SetMessage(string message)
        {
            _message = new GUIString(message, Font.Default, FontColors.DefaultDarkGray_I);
        }
        private void DeleteMessage(GL gl = null)
        {
            _message?.Delete(gl ?? Game.OpenGL);
            _message = null;
        }

        #region Battle replacement

        private void SetBattleReplacementMessage()
        {
            SetMessage(string.Format("You must send in {0} Pokémon.", BattleGUI.Instance.SwitchesRequired));
        }
        private static bool CanUsePositionForBattle(PBEFieldPosition pos)
        {
            return !BattleGUI.Instance.PositionStandBy.Contains(pos) && BattleGUI.Instance.Trainer.OwnsSpot(pos) && BattleGUI.Instance.Trainer.Team.TryGetPokemon(pos) is null;
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
            Engine.Instance.SetCallback(CB_LogicTick);
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
            _textChoicesWindow = new Window(new RelPos2D(0.6f, 0.3f), s, Colors.White);
            RenderChoicesOntoWindow();
            Engine.Instance.SetCallback(CB_Choices);
        }

        #endregion

        #region Field Moves

        private void AddFieldMovesToActions(PartyPokemon pkmn, int index)
        {
            void Add(PBEMove move, Action command)
            {
                string str = PBELocalizedString.GetMoveName(move).English;
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
            Engine.Instance.SetCallback(CB_CantUseFieldMove);
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
        private void Action_BringUpSummary(int index)
        {
            _selectionForSummary = index;
            _fadeTransition = new FadeToColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_FadeOutToSummary);
            Engine.Instance.SetRCallback(RCB_Fading);
        }

        private void BringUpPkmnActions(int index)
        {
            string nickname;
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToLogicTick, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
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

            _textChoices.AddOne("Cancel", CloseChoicesThenGoToLogicTick);
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(new RelPos2D(0.6f, 0.3f), s, Colors.White);
            RenderChoicesOntoWindow();
            SetMessage(string.Format("Do what with {0}?", nickname));
            Engine.Instance.SetCallback(CB_Choices);
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
                DeleteMessage();
            }
            Engine.Instance.SetCallback(CB_LogicTick);
        }
        private void RenderChoicesOntoWindow()
        {
            _textChoices.RenderChoicesOntoWindow(_textChoicesWindow);
        }

        private void CB_Choices()
        {
            _sprites.DoCallbacks();

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
            _sprites.DoCallbacks();

            if (!InputManager.IsPressed(Key.A) && !InputManager.IsPressed(Key.B))
            {
                return;
            }

            int index = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            // Assume Mode.PkmnMenu
            SetMessage(string.Format("Do what with {0}?", _gameParty.Party[index].Nickname));
            Engine.Instance.SetCallback(CB_Choices);
        }
        private void CB_LogicTick()
        {
            _sprites.DoCallbacks();

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

        private void RCB_RenderTick(GL gl)
        {
            // Background
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(222, 50, 60, 255), Renderer.Color(190, 40, 50, 255), Renderer.Color(255, 180, 200, 255));
            GLHelper.ClearColor(gl, ColorF.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            int dstW = (int)GLHelper.CurrentWidth;
            int dstH = (int)GLHelper.CurrentHeight;
            for (int i = 0; i < _members.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = col == 0 ? dstW / 40 : (dstW / 2) + (dstW / 40);
                int y = row * (dstH / 4) + (dstH / 20);
                _members[i].Render(new Pos2D(x, y), col == _selectionX && row == _selectionY);
            }

            // Back button
            if (_allowBack)
            {
                GUIRenderer.Instance.FillRectangle(_selectionY == -1 ? ColorF.FromRGB(96, 48, 48) : ColorF.FromRGB(48, 48, 48), new Rect2D(Pos2D.FromRelative(0.5f, 0.8f), Size2D.FromRelative(0.5f, 0.2f)));
                _backStr.Render(gl, Pos2D.FromRelative(0.5f, 0.8f));
            }

            if (_message is not null)
            {
                GUIRenderer.Instance.FillRectangle(ColorF.FromRGB(200, 200, 200), new Rect2D(Pos2D.FromRelative(0f, 0.8f), Size2D.FromRelative(0.5f, 0.2f)));
                _message.Render(gl, Pos2D.FromRelative(0f, 0.8f));
            }

            Window.RenderAll();
        }
    }
}
